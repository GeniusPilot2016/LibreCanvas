// LibreCanvas - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;

namespace Carpathia
{
    public enum CatAnimation
    {
        Idle,
        Happy,
        LookLeftToRight,
        StretchYawn,
        WalkCycle,
        Wink,
        BubbleGumBlow,
        BubbleGumPop,
        WatchingButterfly,
        ButterflyLandsOnEar,
        ButterflyChase,
        SunglassesSparkle,
        HappyHearts
    }

    // Sunny the Cat is the mascot of LibreCanvas that born from an AI-generated image from this program using "A cute orange cat wearing sunglasses at the beach" prompt in resources.resx
    public partial class SunnyTheCat : UserControl
    {
        private int frame = 0;
        private Bitmap[] frames;
        private Region[] frameRegions;

        private Dictionary<CatAnimation, Region[]> regionCache = new Dictionary<CatAnimation, Region[]>();
        private Dictionary<CatAnimation, Bitmap[]> animationMap;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool WasDragged { get; set; } = false;

        private CatAnimation currentAnimation = CatAnimation.Idle;
        private bool returnToIdleAfterOnce = false;

        // Bubble_Gum_Pop has 14 frames at 12 fps. Index 8 is the ninth frame;
        // adjust this if the visible burst occurs at a different frame in your PNGs.
        private const int BubblePopSoundFrame = 8;
        private bool bubblePopSoundPlayed;
        private System.Media.SoundPlayer poppingSoundPlayer;

        // Stretch_Yawn has eight frames. Start the yawn sound as the mouth opens.
        private const int YawnSoundFrame = 1;
        private bool yawnSoundPlayed;
        private System.Media.SoundPlayer yawningSoundPlayer;

        // Random idle behaviors are separate from click reactions.
        private readonly Random rng = new Random();
        private readonly System.Windows.Forms.Timer randomBehaviorTimer = new System.Windows.Forms.Timer();
        private int originalFrameInterval = 100; // Value from timer1 in the designer.
        // New animations play automatically while idle; wink remains click-only.
        private readonly CatAnimation[] idleRandomPool = new CatAnimation[]
        {
            CatAnimation.LookLeftToRight,
            CatAnimation.StretchYawn,
            CatAnimation.BubbleGumBlow,
            CatAnimation.BubbleGumPop,
            CatAnimation.WatchingButterfly,
            CatAnimation.ButterflyLandsOnEar,
            CatAnimation.ButterflyChase,
            CatAnimation.SunglassesSparkle,
            CatAnimation.HappyHearts
        };

        Bitmap[] idleFrames = new Bitmap[]
        {
            Properties.Resources.Idle_01,
            Properties.Resources.Idle_02,
            Properties.Resources.Idle_03,
            Properties.Resources.Idle_04,
            Properties.Resources.Idle_05,
            Properties.Resources.Idle_06,
            Properties.Resources.Idle_07
        };

        Bitmap[] happyFrames = new Bitmap[]
        {
            Properties.Resources.Happy_01,
            Properties.Resources.Happy_02,
            Properties.Resources.Happy_03
        };

        Bitmap[] lookLeftToRightFrames = new Bitmap[]
        {
            Properties.Resources.Look_Left_to_Right_01,
            Properties.Resources.Look_Left_to_Right_02,
            Properties.Resources.Look_Left_to_Right_03,
            Properties.Resources.Look_Left_to_Right_04,
            Properties.Resources.Look_Left_to_Right_05,
            Properties.Resources.Look_Left_to_Right_06,
            Properties.Resources.Look_Left_to_Right_07,
            Properties.Resources.Look_Left_to_Right_08,
        };

        Bitmap[] stretchYawnFrames = new Bitmap[]
        {
            Properties.Resources.Stretch_Yawn_01,
            Properties.Resources.Stretch_Yawn_02,
            Properties.Resources.Stretch_Yawn_03,
            Properties.Resources.Stretch_Yawn_04,
            Properties.Resources.Stretch_Yawn_05,
            Properties.Resources.Stretch_Yawn_06,
            Properties.Resources.Stretch_Yawn_07,
            Properties.Resources.Stretch_Yawn_08,
        };

        Bitmap[] walkCycleFrames = new Bitmap[]
        {
            Properties.Resources.Walk_Cycle_01,
            Properties.Resources.Walk_Cycle_02,
            Properties.Resources.Walk_Cycle_03,
            Properties.Resources.Walk_Cycle_04,
            Properties.Resources.Walk_Cycle_05,
            Properties.Resources.Walk_Cycle_06,
            Properties.Resources.Walk_Cycle_07,
            Properties.Resources.Walk_Cycle_08,
        };

        Bitmap[] winkFrames = new Bitmap[]
        {
            Properties.Resources.Wink_01,
            Properties.Resources.Wink_02,
            Properties.Resources.Wink_03,
            Properties.Resources.Wink_04,
            Properties.Resources.Wink_05,
            Properties.Resources.Wink_06
        };

        public SunnyTheCat()
        {
            InitializeComponent();
            originalFrameInterval = timer1.Interval;

            // Keep the SoundPlayer alive until this control is disposed. SoundPlayer.Play()
            // is asynchronous, so disposing it immediately could interrupt the sound.
            this.Disposed += (s, e) =>
            {
                if (poppingSoundPlayer != null)
                {
                    poppingSoundPlayer.Dispose();
                    poppingSoundPlayer = null;
                }
                if (yawningSoundPlayer != null)
                {
                    yawningSoundPlayer.Dispose();
                    yawningSoundPlayer = null;
                }
            };

            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.Transparent;
            this.BackColor = Color.Transparent;

            // Forward a genuine click, but not the click WinForms may raise after dragging.
            // The pictureBox1_Click handler below owns the sound and reaction.
            pictureBox1.Click += (s, e) =>
            {
                if (!WasDragged)
                    this.OnClick(e);
            };
            pictureBox1.MouseEnter += (s, e) => this.OnMouseEnter(e);
            pictureBox1.MouseLeave += (s, e) => this.OnMouseLeave(e);

            animationMap = new Dictionary<CatAnimation, Bitmap[]>
            {
                { CatAnimation.Idle, idleFrames },
                { CatAnimation.Happy, happyFrames },
                { CatAnimation.LookLeftToRight, lookLeftToRightFrames },
                { CatAnimation.StretchYawn, stretchYawnFrames },
                { CatAnimation.WalkCycle, walkCycleFrames },
                { CatAnimation.Wink, winkFrames }
                // New animations are loaded on demand when first played.
            };

            // Set up the random behavior timer to trigger random idle animations.
            randomBehaviorTimer.Tick += RandomBehaviorTimer_Tick;
            ScheduleNextRandomBehavior();
            randomBehaviorTimer.Start();

            SetAnimation(CatAnimation.Idle);
            timer1.Start();
        }

        public CatAnimation CurrentAnimation => currentAnimation;

        /// <summary>
        /// Gets or sets whether Sunny will perform random idle animations while the user is not interacting with it.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableRandomIdleAnimations
        { 
            get => randomBehaviorTimer.Enabled;
            set
            {
                if (value)
                {
                    ScheduleNextRandomBehavior();
                    randomBehaviorTimer.Start();
                }
                else
                {
                    randomBehaviorTimer.Stop();
                }
            }
        }

        private void PlayPoppingSound()
        {
            try
            {
                // Retain the resource stream/player for the entire playback.
                if (poppingSoundPlayer == null)
                {
                    poppingSoundPlayer = new System.Media.SoundPlayer(Properties.Resources.bubble_pop);
                    poppingSoundPlayer.Load();
                }

                poppingSoundPlayer.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Bubble pop sound failed: " + ex);
            }
        }

        private void PlayYawningSound()
        {
            try
            {
                // Keep the player alive for asynchronous playback.
                if (yawningSoundPlayer == null)
                {
                    yawningSoundPlayer = new System.Media.SoundPlayer(Properties.Resources.yawn);
                    yawningSoundPlayer.Load();
                }
                yawningSoundPlayer.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void PlayMeowSound()
        {
            try
            {
                using (var player = new System.Media.SoundPlayer(Properties.Resources.meow_sound_effect))
                {
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void PlayPurrSound()
        {
            try
            {
                using (var player = new System.Media.SoundPlayer(Properties.Resources.purr_sound_effect))
                {
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ScheduleNextRandomBehavior()
        {
            // 4 ile 10 saniye arasında rastgele bir süre belirle
            randomBehaviorTimer.Interval = rng.Next(4000, 10001);
        }

        private void RandomBehaviorTimer_Tick(object sender, EventArgs e)
        {
            if(this.DesignMode)
                return; // Don't trigger random behaviors in the designer.
            if (currentAnimation == CatAnimation.Idle && idleRandomPool.Length > 0)
            {
                CatAnimation chosen = idleRandomPool[rng.Next(idleRandomPool.Length)];
                SetAnimation(chosen, playOnce: true);
            }

            ScheduleNextRandomBehavior();
        }

        /// <summary>
        /// Click reactions ONLY: meow + Happy, purr + LookLeftToRight, or Wink.
        /// New animations belong to the random idle behavior pool, not here.
        /// </summary>

        private bool reactionInProgress = false;

        public async void PlayRandomReaction()
        {
            if (reactionInProgress || IsDisposed || WasDragged)
                return;

            reactionInProgress = true;

            try
            {
                int reaction = rng.Next(3);
                string message;

                switch (reaction)
                {
                    case 0:
                        message = "Meow! I'm Sunny the Cat!";
                        break;
                    case 1:
                        message = "Purr... I like that!";
                        break;
                    default:
                        message = "Wink! I'm feeling playful!";
                        break;
                }

                toolTip1.Hide(pictureBox1);

                // Position tooltip farther above Sunny.
                toolTip1.Show(
                    message,
                    pictureBox1,
                    pictureBox1.Width / 2,
                    -40,
                    1200
                );

                await Task.Delay(500);

                if (IsDisposed || Disposing)
                    return;

                switch (reaction)
                {
                    case 0:
                        PlayMeowSound();
                        SetAnimation(CatAnimation.Happy, playOnce: true);
                        break;

                    case 1:
                        PlayPurrSound();
                        SetAnimation(CatAnimation.LookLeftToRight, playOnce: true);
                        break;

                    default:
                        SetAnimation(CatAnimation.Wink, playOnce: true);
                        break;
                }

                // Let the selected animation finish.
                int animationDuration =
                    frames.Length * timer1.Interval + 100;

                await Task.Delay(animationDuration);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                    toolTip1.Hide(pictureBox1);

                reactionInProgress = false;
            }
        }


        /// <summary>
        /// Loads the PNG frames embedded in Properties/Resources.resx.
        /// The installer registers all 112 images under their exact filenames.
        /// </summary>
        private static Bitmap[] LoadEmbeddedFrames(string prefix, int count)
        {
            Bitmap[] loaded = new Bitmap[count];
            for (int i = 0; i < count; i++)
            {
                string resourceName = prefix + "_" + (i + 1).ToString("D2");
                Bitmap image = Properties.Resources.ResourceManager.GetObject(resourceName) as Bitmap;
                if (image == null)
                    throw new InvalidOperationException(
                        "Sunny animation resource not found: " + resourceName +
                        ". Run Install-SunnyAnimations.ps1 to add the new PNG resources.");
                loaded[i] = image;
            }
            return loaded;
        }

        private static Bitmap[] LoadNewAnimation(CatAnimation animation)
        {
            switch (animation)
            {
                case CatAnimation.BubbleGumBlow: return LoadEmbeddedFrames("Bubble_Gum_Blow", 16);
                case CatAnimation.BubbleGumPop: return LoadEmbeddedFrames("Bubble_Gum_Pop", 14);
                case CatAnimation.WatchingButterfly: return LoadEmbeddedFrames("Watching_Butterfly", 18);
                case CatAnimation.ButterflyLandsOnEar: return LoadEmbeddedFrames("Butterfly_Lands_On_Ear", 18);
                case CatAnimation.ButterflyChase: return LoadEmbeddedFrames("Butterfly_Chase", 16);
                case CatAnimation.SunglassesSparkle: return LoadEmbeddedFrames("Sunglasses_Sparkle", 14);
                case CatAnimation.HappyHearts: return LoadEmbeddedFrames("Happy_Hearts", 16);
                default: return null;
            }
        }

        private int GetFrameInterval(CatAnimation animation)
        {
            // New animations use the FPS specified in animation_manifest.json.
            switch (animation)
            {
                case CatAnimation.BubbleGumBlow: return 111; // 9 fps
                case CatAnimation.BubbleGumPop: return 83; // 12 fps
                case CatAnimation.WatchingButterfly: return 100; // 10 fps
                case CatAnimation.ButterflyLandsOnEar: return 100; // 10 fps
                case CatAnimation.ButterflyChase: return 83; // 12 fps
                case CatAnimation.SunglassesSparkle: return 100; // 10 fps
                case CatAnimation.HappyHearts: return 100; // 10 fps
                default: return originalFrameInterval;
            }
        }

        /// <summary>
        /// Changes the current animation to the specified one. If playOnce is true, the animation will return to Idle after finishing.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="playOnce"></param>
        public void SetAnimation(CatAnimation animation, bool playOnce = false)
        {
            if (animationMap == null) return;

            Bitmap[] selectedFrames;
            if (!animationMap.TryGetValue(animation, out selectedFrames))
            {
                selectedFrames = LoadNewAnimation(animation);
                if (selectedFrames == null) return;
                animationMap[animation] = selectedFrames;
            }
            if (selectedFrames.Length == 0) return;

            currentAnimation = animation;
            // Pop and ear-landing animations are one-shot according to the supplied manifest.
            returnToIdleAfterOnce = playOnce ||
                animation == CatAnimation.BubbleGumPop ||
                animation == CatAnimation.ButterflyLandsOnEar;
            frames = selectedFrames;
            frame = 0;
            bubblePopSoundPlayed = false;
            yawnSoundPlayed = false;
            frameRegions = null;
            timer1.Interval = GetFrameInterval(animation);

            BuildRegions();
            ApplyFrame(0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClearRegionCache();
            BuildRegions();
            ApplyFrame(frame);
        }

        private void ClearRegionCache()
        {
            foreach (var regArray in regionCache.Values)
            {
                if (regArray == null) continue;
                foreach (var r in regArray) r?.Dispose();
            }
            regionCache.Clear();
        }

        private void BuildRegions()
        {
            if (frames == null || this.Width <= 0 || this.Height <= 0)
                return;

            if (regionCache.TryGetValue(currentAnimation, out var cachedRegions))
            {
                frameRegions = cachedRegions;
                return;
            }

            Region[] newRegions = new Region[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                newRegions[i] = CreateRegionFromBitmap(frames[i], this.Width, this.Height);
            }

            regionCache[currentAnimation] = newRegions;
            frameRegions = newRegions;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.DesignMode)
                return; // Don't advance frames in the designer, which can cause "haunted" program phenomenon.
            if (frames == null || frames.Length == 0) return;

            int nextFrame = frame + 1;

            if (nextFrame >= frames.Length && returnToIdleAfterOnce)
            {
                SetAnimation(CatAnimation.Idle, playOnce: false);
                return;
            }

            frame = nextFrame % frames.Length;
            ApplyFrame(frame);

            // Trigger only on the actual pop frame, never on resize or before
            // the bubble bursts. Each animation run plays this sound once.
            if (currentAnimation == CatAnimation.BubbleGumPop &&
                frame == BubblePopSoundFrame && !bubblePopSoundPlayed)
            {
                bubblePopSoundPlayed = true;
                PlayPoppingSound();
            }

            // Play once per StretchYawn animation, synchronized with frame 4.
            if (currentAnimation == CatAnimation.StretchYawn &&
                frame == YawnSoundFrame && !yawnSoundPlayed)
            {
                yawnSoundPlayed = true;
                PlayYawningSound();
            }
        }

        private void ApplyFrame(int index)
        {
            if (frames == null || frames[index] == null) return;

            pictureBox1.Image = frames[index];

            if (frameRegions != null && frameRegions[index] != null)
            {
                this.Region = frameRegions[index].Clone();
            }
        }

        private Region CreateRegionFromBitmap(Bitmap bitmap, int targetWidth, int targetHeight)
        {
            float scale = Math.Min((float)targetWidth / bitmap.Width,
                                   (float)targetHeight / bitmap.Height);
            float offsetX = (targetWidth - bitmap.Width * scale) / 2f;
            float offsetY = (targetHeight - bitmap.Height * scale) / 2f;

            using (GraphicsPath path = new GraphicsPath())
            using (Bitmap argb = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(argb))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImageUnscaled(bitmap, 0, 0);
                }

                Rectangle imageRect = new Rectangle(0, 0, argb.Width, argb.Height);
                BitmapData data = argb.LockBits(imageRect, ImageLockMode.ReadOnly,
                                                PixelFormat.Format32bppArgb);
                try
                {
                    byte[] row = new byte[Math.Abs(data.Stride)];
                    for (int y = 0; y < argb.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                        int startX = -1;
                        for (int x = 0; x < argb.Width; x++)
                        {
                            bool isOpaque = row[x * 4 + 3] > 200;
                            if (isOpaque && startX < 0)
                                startX = x;
                            else if (!isOpaque && startX >= 0)
                            {
                                path.AddRectangle(new RectangleF(offsetX + startX * scale,
                                    offsetY + y * scale, (x - startX) * scale, scale));
                                startX = -1;
                            }
                        }
                        if (startX >= 0)
                            path.AddRectangle(new RectangleF(offsetX + startX * scale,
                                offsetY + y * scale, (argb.Width - startX) * scale, scale));
                    }
                }
                finally
                {
                    argb.UnlockBits(data);
                }

                if (path.PointCount == 0)
                    path.AddRectangle(new Rectangle(0, 0, 1, 1));
                return new Region(path);
            }
        }

        private void SunnyTheCat_Load(object sender, EventArgs e)
        {
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Dragging is not a click and must not trigger a reaction.
            if (WasDragged)
                return;

            // Exactly three click outcomes: meow, purr, or wink.
            PlayRandomReaction();
        }

        private void SunnyTheCat_VisibleChanged(object sender, EventArgs e)
        {
            if(this.Visible)
            {
                timer1.Start();
                randomBehaviorTimer.Start();
            }
            else
            {
                timer1.Stop();
                randomBehaviorTimer.Stop();
            }
        }
    }
}
