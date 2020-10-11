﻿using Microsoft.Xna.Framework.Audio;
using MonoSound.Filters;
using MonoSound.Filters.Instances;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonoSound{
	/// <summary>
	/// The class where all sound filtering is handled through.
	/// </summary>
	public static class MonoSoundManager{
		private static Dictionary<int, Filter> customFilters;

		/// <summary>
		/// The next filter ID should one be registered.  Automatically assigned to new filters
		/// </summary>
		public static int NextFilterID{ get; private set; }

		/// <summary>
		/// Whether the Echo filter is allowed to generate over 30 seconds' worth of samples for a sound, which can happen when using high Delay and low Decay parameters.
		/// </summary>
		public static bool AllowEchoOversampling{ get; set; }

		/// <summary>
		/// If enabled, this folder path will be where filtered sounds are saved to. This property should be set after <seealso cref="Init"/> is called.
		/// </summary>
		public static string LogDirectory{ get; set; }

		/// <summary>
		/// Enables or disables the saving of filtered sounds.  Set <seealso cref="LogDirectory"/> to where the filtered sounds will be saved to
		/// </summary>
		public static bool LogFilters{ get; set; }

		private static bool initialized = false;

		/// <summary>
		/// Initializes MonoSound
		/// </summary>
		public static void Init(){
			SoundFilterManager.Init();

			customFilters = new Dictionary<int, Filter>();

			NextFilterID = 0;

			initialized = true;

			if(Directory.Exists(LogDirectory))
				Directory.Delete(LogDirectory, true);
		}

		/// <summary>
		/// Deinitializes MonoSound
		/// </summary>
		public static void DeInit(){
			SoundFilterManager.DeInit();

			FilterSimulations.bqrFilter?.Free();
			FilterSimulations.echFilter?.Free();
			FilterSimulations.revFilter?.Free();

			customFilters = null;

			NextFilterID = 0;

			initialized = false;
		}

		/// <summary>
		/// Sets how many filters can be loaded at once
		/// </summary>
		public static void SetMaxFilterCount(int count){
			if(count < 50)
				throw new ArgumentException("Value was too small.", "count");
			if(count > 1000)
				throw new ArgumentException("Value was too large.", "count");

			SoundFilterManager.Max_Filters_Loaded = count;
		}

		/// <summary>
		/// Clears any stored filters
		/// </summary>
		public static void ClearFilters() => SoundFilterManager.Clear();

		private static void ThrowIfNotInitialized(){
			if(!initialized)
				throw new InvalidOperationException("MonoSound has not initialized yet!");
		}

		/// <summary>
		/// Applies the wanted filter to the sound file.
		/// </summary>
		/// <param name="file">The path to the sound file. Extension required.</param>
		/// <param name="filterID">The ID of the filter to use.</param>
		public static SoundEffect GetFilteredEffect(string file, int filterID){
			ThrowIfNotInitialized();

			if(!customFilters.ContainsKey(filterID))
				throw new ArgumentException("Given Filter ID does not correspond to a registered sound filter.", "filterID");

			return SoundFilterManager.CreateFilteredSFX(file, customFilters[filterID]);
		}

		/// <summary>
		/// Applies the wanted filters to the sound file in the order requested.
		/// </summary>
		/// <param name="file">The path to the sound file. Extension required.</param>
		/// <param name="filterIDs">The list of filter IDs to use.</param>
		/// <returns></returns>
		public static SoundEffect GetMultiFilteredEffect(string file, params int[] filterIDs){
			ThrowIfNotInitialized();

			if(!AllFiltersIDsExist(filterIDs))
				throw new ArgumentException("One of the given Filter IDs does not correspond to a registered sound filter.", "filterIDs");

			return SoundFilterManager.CreateFilteredSFX(file, GetFiltersFromIDs(filterIDs));
		}

		private static Filter[] GetFiltersFromIDs(int[] ids){
			//LINQ bad
			Filter[] filters = new Filter[ids.Length];
			for(int i = 0; i < ids.Length; i++)
				filters[i] = customFilters[ids[i]];
			return filters;
		}

		private static bool AllFiltersIDsExist(int[] ids){
			bool success = true;

			for(int i = 0; i < ids.Length; i++)
				if(!customFilters.ContainsKey(ids[i]))
					success = false;

			return success;
		}

		/// <summary>
		/// Registers a Biquad Resonant filter.
		/// </summary>
		/// <param name="type">The filter type to use. Must either be <seealso cref="SoundFilterType.LowPass"/>, <seealso cref="SoundFilterType.BandPass"/> or <seealso cref="SoundFilterType.HighPass"/></param>
		/// <param name="strength">How strong the filter effect is. 0 = no effect, 1 = full effect</param>
		/// <param name="frequencyCap">The frequency parameter. Expected values are between 1000 and 8000</param>
		/// <param name="resonance">The resonance parameter. Expected values are between 2 and 20</param>
		/// <returns></returns>
		public static int RegisterBiquadResonantFilter(SoundFilterType type, float strength, float frequencyCap, float resonance){
			ThrowIfNotInitialized();

			int filterType;
			switch(type){
				case SoundFilterType.LowPass:
					filterType = BiquadResonantFilter.LOWPASS;
					break;
				case SoundFilterType.BandPass:
					filterType = BiquadResonantFilter.BANDPASS;
					break;
				case SoundFilterType.HighPass:
					filterType = BiquadResonantFilter.HIGHPASS;
					break;
				default:
					throw new ArgumentException("Given type wasn't a valid Biquad Resonant Filter type.", "type");
			}

			BiquadResonantFilter bqf = new BiquadResonantFilter();
			bqf.setParams(filterType, frequencyCap, resonance);
			bqf.SetStrength(strength);
			bqf.ID = NextFilterID++;
			bqf.type = type;

			customFilters.Add(bqf.ID, bqf);

			return bqf.ID;
		}

		/// <summary>
		/// Registers an Echo filter.
		/// </summary>
		/// <param name="strength">How strong the filter effect is. 0 = no effect, 1 = full effect</param>
		/// <param name="initialDelay">The initial delay in seconds before the echo starts</param>
		/// <param name="decayFactor">The factor applied to the volume of each successive echo.  Expected values are between 0 and 1</param>
		/// <param name="filterStrength">How strongly this filter will prefer using old samples over new samples when processing the sound.  Expected values are between 0 (no effect) and 1 (full effect)</param>
		/// <returns></returns>
		public static int RegisterEchoFilter(float strength, float initialDelay, float decayFactor, float filterStrength){
			ThrowIfNotInitialized();

			EchoFilter ech = new EchoFilter();
			ech.setParams(initialDelay, decayFactor, filterStrength);
			ech.SetStrength(strength);
			ech.ID = NextFilterID++;
			ech.type = SoundFilterType.Echo;

			customFilters.Add(ech.ID, ech);

			return ech.ID;
		}

		/// <summary>
		/// Registers a Reverb filter
		/// </summary>
		/// <param name="filterStrength">How strong the filter effect is. 0 = no effect, 1 = full effect</param>
		/// <param name="lowFrequencyReverbStrength">How much the filter affects low frequencies. 0 = fast decaying, 1 = slow decaying. Defaults to 0.5</param>
		/// <param name="highFrequencyReverbStrength">How much the filter affects high frequencies. 0 = fast decaying, 1 = slow decaying. Defaults to 0.5</param>
		/// <param name="reverbStrength">How strong the reverb effect is. Expected values are between 0 and 1. Defaults to 1</param>
		/// <returns></returns>
		public static int RegisterReverbFilter(float filterStrength, float lowFrequencyReverbStrength, float highFrequencyReverbStrength, float reverbStrength){
			ThrowIfNotInitialized();

			FreeverbFilter rev = new FreeverbFilter();
			rev.setParams(0, lowFrequencyReverbStrength, highFrequencyReverbStrength, reverbStrength);
			rev.SetStrength(filterStrength);
			rev.ID = NextFilterID++;
			rev.type = SoundFilterType.Reverb;

			customFilters.Add(rev.ID, rev);

			return rev.ID;
		}
	}
}