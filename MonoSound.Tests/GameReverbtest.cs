
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoSound.Filters;
using MonoSound.Filters.Instances;
using MonoSound.Streaming;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MonoSound.Tests {
	public class Game1 : Game {
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;



		public Game1() {
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize() {
			// TODO: Add your initialization logic here

	

		
			base.Initialize();
		}



		SoundEffectInstance r2;


        protected override void LoadContent() {
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// TODO: use this.Content to load your game content here

			MonoSoundLibrary.Init();

		    

			lowPass = FilterLoader.RegisterBiquadResonantFilter(SoundFilterType.LowPass, 1, 1000, 5);
			highPass = FilterLoader.RegisterBiquadResonantFilter(SoundFilterType.HighPass, 1, 1500, 8);
			bandPass = FilterLoader.RegisterBiquadResonantFilter(SoundFilterType.BandPass, 1, 2000, 3);

			Controls.StreamBufferLengthInSeconds = 0.5;


             int reverb = FilterLoader.RegisterReverbFilter(1f, lowFrequencyReverbStrength: 1f, highFrequencyReverbStrength: 1f, reverbStrength: .1f);



			int echo = FilterLoader.RegisterEchoFilter(1f, 0.2f,0.1f, 1f);


			List<int> effects =new List<int>();

			effects.Add(reverb); 
			effects.Add(echo);
		//	effects.Add(highPass);

			
            // GetFilteredEffect() can use either a relative path or an absolute path.  Files not supported will throw an exception.
  //       SoundEffect reverbEffect = EffectLoader.GetFilteredEffect("Content/chill-mono.ogg", reverb);

            //	 SoundEffect reverbEffect = SoundEffect.FromFile("Content/Splash.wav");
            //       SoundEffect reverbEffect = SoundEffect.FromFile("Content/Splash.wav");
            //         SoundEffect reverbEffect = EffectLoader.GetFilteredEffect("Content/50cal.wav", reverb);

 //            SoundEffect reverbEffect = EffectLoader.GetFilteredEffect("Content/MunchMunch-resaved.wav", echo);

            SoundEffect reverbEffect = EffectLoader.GetMultiFilteredEffect("Content/MunchMunch-resaved.wav", effects.ToArray());

    //        CreateFilteredSFX

            r2 =reverbEffect.CreateInstance();
	

		}

		static KeyboardState kb, oldKb;
		static SoundEffect sfx, song, filteredSfx;
		static SoundEffectInstance songInstance, filteredSfxInstance;
		static StreamPackage streamedSound;

		static int lowPass, highPass, bandPass;

		static int timer;

		static readonly List<Keys> currentSequence = new List<Keys>();

		protected override void OnExiting(object sender, EventArgs args) {
			MonoSoundLibrary.DeInit();
		}
		float parm = 0;
		protected override void Update(GameTime gameTime) {
			oldKb = kb;
			kb = Keyboard.GetState();

			var input = kb.GetPressedKeys().Except(oldKb.GetPressedKeys());




			if (input.Any(x => x==Keys.Space))
				{

				   r2.Pitch+=0.01f;
                r2.Play();


		
            }	
			
			parm+=0.3f;
				MonoSoundLibrary.CustomFilters[4].SetFilterParameter(0, 0.1f);//todo this is not virtual

            //// Different pan testing -> Mono Channel
            //if (songInstance != null) {
            //	float revolutionsPerSecond = 0.6f;
            //	float sin = MathF.Sin(MathHelper.ToRadians(timer * 6f * revolutionsPerSecond));
			 
            //	songInstance.Pan = sin;
            //}

            timer++;

			base.Update(gameTime);
		}

		private static void PlayStereoPanning() {
			// Different pan testing (fails due to the sound using Stereo)
			sfx ??= EffectLoader.GetEffect("Content/MunchMunch-resaved2.wav");

			float revolutionsPerSecond = 0.3f;
			float sin = MathF.Sin(MathHelper.ToRadians(timer * 6f * revolutionsPerSecond));

			sfx.Play(1, 0, sin);

		
		}

		private static void PlayMonoPanning() {
			// Different pan testing (succeeds due to the sound using Mono)
			if (streamedSound != null)
				StreamLoader.FreeStreamedSound(ref streamedSound);

			song ??= EffectLoader.GetEffect("Content/chill-mono.ogg");

			if (songInstance is null) {
				songInstance = song.CreateInstance();
				songInstance.Volume = 0.3f;
			} else {
				songInstance.Dispose();
				songInstance = null;
			}

			songInstance.Play();
		}

		private static void PlayStreamedSong() {
			// Streamed audio testing
			if (songInstance != null) {
				songInstance.Dispose();
				songInstance = null;
			}

			if (streamedSound is null) {
				streamedSound = StreamLoader.GetStreamedSound("Content/chill-mono.ogg", looping: true);
				streamedSound.PlayingSound.Volume = 0.3f;
			}

			streamedSound.PlayingSound.Play();
		}

		private static void StopStreamedSong() {
			if (streamedSound != null)
				StreamLoader.FreeStreamedSound(ref streamedSound);
		}

		private static void ClearStreamedSongFilters() {
			// Applying filters to streamed audio testing
			streamedSound?.ApplyFilters(ids: null);
		}

		private static void ApplyLowPassFilterToStreamedSong() {
			// Applying filters to streamed audio testing
			streamedSound?.ApplyFilters(ids: lowPass);
		}

		private static void ApplyHighPassFilterToStreamedSong() {
			// Applying filters to streamed audio testing
			streamedSound?.ApplyFilters(ids: highPass);
		}

		private static void ApplyBandPassFilterToStreamedSong() {
			// Applying filters to streamed audio testing
			streamedSound?.ApplyFilters(ids: bandPass);

		
		}

		private static void PlayLowPassFilteredSound() {
			// Audio with filters testing
			filteredSfx = EffectLoader.GetFilteredEffect("Content/spooky.mp3", lowPass);

			filteredSfxInstance?.Dispose();
			filteredSfxInstance = filteredSfx.CreateInstance();

			filteredSfxInstance.Play();
		}

		private static void PlayHighPassFilteredSound() {
			// Audio with filters testing
			filteredSfx = EffectLoader.GetFilteredEffect("Content/spooky.mp3", highPass);

			filteredSfxInstance?.Dispose();
			filteredSfxInstance = filteredSfx.CreateInstance();

			filteredSfxInstance.Play();
		}

		private static void PlayBandPassFilteredSound() {
			// Audio with filters testing
			filteredSfx = EffectLoader.GetFilteredEffect("Content/spooky.mp3", bandPass);

			filteredSfxInstance?.Dispose();
			filteredSfxInstance = filteredSfx.CreateInstance();

			filteredSfxInstance.Play();
		}

		static Process ThisProcess;

		protected override void Draw(GameTime gameTime) {
			ThisProcess ??= Process.GetCurrentProcess();

			if (timer % 30 == 0)
				ThisProcess.Refresh();

			GraphicsDevice.Clear(Color.CornflowerBlue);

			_spriteBatch.Begin();


		

	

			_spriteBatch.End();

			base.Draw(gameTime);
		}

		private static string ByteCountToLargeRepresentation(long bytes) {
			string[] storageSizes = new string[] { "B", "kB", "MB", "GB" };
			int sizeLog = (int)Math.Log(bytes, 1000);
			double sizePow = (long)Math.Pow(1000, sizeLog);
			string size = storageSizes[sizeLog];

			return $"{bytes / sizePow:N3}{size}";
		}
	}
}
