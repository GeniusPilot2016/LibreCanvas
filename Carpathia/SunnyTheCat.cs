using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        Wink
    }

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

        // Rastgele animasyon yönetimi için değişkenler
        private readonly Random rng = new Random();
        private readonly System.Windows.Forms.Timer randomBehaviorTimer = new System.Windows.Forms.Timer();
        private readonly CatAnimation[] idleRandomPool = new CatAnimation[]
        {
            CatAnimation.Wink,
            CatAnimation.LookLeftToRight,
            CatAnimation.StretchYawn
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
            };

            // Rastgele boşta davranış zamanlayıcısını kur
            randomBehaviorTimer.Tick += RandomBehaviorTimer_Tick;
            ScheduleNextRandomBehavior();
            randomBehaviorTimer.Start();

            SetAnimation(CatAnimation.Idle);
            timer1.Start();
        }

        public CatAnimation CurrentAnimation => currentAnimation;

        /// <summary>
        /// Boşta rastgele animasyon oynatma özelliğini açıp kapatır.
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

        private void ScheduleNextRandomBehavior()
        {
            // 4 ile 10 saniye arasında rastgele bir süre belirle
            randomBehaviorTimer.Interval = rng.Next(4000, 10001);
        }

        private void RandomBehaviorTimer_Tick(object sender, EventArgs e)
        {
            // Sadece kedi Idle (boşta) durumundaysa rastgele hareket yap
            if (currentAnimation == CatAnimation.Idle && idleRandomPool.Length > 0)
            {
                CatAnimation chosen = idleRandomPool[rng.Next(idleRandomPool.Length)];
                SetAnimation(chosen, playOnce: true);
            }

            ScheduleNextRandomBehavior();
        }

        /// <summary>
        /// Tıklama gibi olaylarda rastgele neşeli bir tepki (Happy veya Wink) oynatır.
        /// </summary>
        public void PlayRandomReaction()
        {
            CatAnimation[] reactions = { CatAnimation.Happy, CatAnimation.Wink };
            CatAnimation chosen = reactions[rng.Next(reactions.Length)];
            SetAnimation(chosen, playOnce: true);
        }

        /// <summary>
        /// Kedinin animasyonunu değiştirir.
        /// </summary>
        /// <param name="animation">Oynatılacak animasyon</param>
        /// <param name="playOnce">True ise animasyon 1 kez oynar ve otomatik Idle'a döner</param>
        public void SetAnimation(CatAnimation animation, bool playOnce = false)
        {
            if (!animationMap.ContainsKey(animation)) return;

            currentAnimation = animation;
            returnToIdleAfterOnce = playOnce;
            frames = animationMap[animation];
            frame = 0;

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
            if (frames == null || frames.Length == 0) return;

            int nextFrame = frame + 1;

            if (nextFrame >= frames.Length && returnToIdleAfterOnce)
            {
                SetAnimation(CatAnimation.Idle, playOnce: false);
                return;
            }

            frame = nextFrame % frames.Length;
            ApplyFrame(frame);
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
            GraphicsPath path = new GraphicsPath();

            float ratioX = (float)targetWidth / bitmap.Width;
            float ratioY = (float)targetHeight / bitmap.Height;
            float scale = Math.Min(ratioX, ratioY);

            float drawWidth = bitmap.Width * scale;
            float drawHeight = bitmap.Height * scale;
            float offsetX = (targetWidth - drawWidth) / 2f;
            float offsetY = (targetHeight - drawHeight) / 2f;

            for (int y = 0; y < bitmap.Height; y++)
            {
                int startX = -1;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    bool isOpaque = bitmap.GetPixel(x, y).A > 200;

                    if (isOpaque && startX == -1)
                    {
                        startX = x;
                    }
                    else if (!isOpaque && startX != -1)
                    {
                        path.AddRectangle(new RectangleF(
                            offsetX + (startX * scale),
                            offsetY + (y * scale),
                            (x - startX) * scale,
                            scale));
                        startX = -1;
                    }
                }

                if (startX != -1)
                {
                    path.AddRectangle(new RectangleF(
                        offsetX + (startX * scale),
                        offsetY + (y * scale),
                        (bitmap.Width - startX) * scale,
                        scale));
                }
            }

            if (path.PointCount == 0)
            {
                path.AddRectangle(new Rectangle(0, 0, 1, 1));
            }

            Region region = new Region(path);
            path.Dispose();
            return region;
        }

        private void SunnyTheCat_Load(object sender, EventArgs e)
        {
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // A drag can still be followed by a Click event. Ignore it entirely.
            // WasDragged is cleared on the *next* left MouseDown by MakeDraggable.
            if (WasDragged)
                return;

            toolTip1.Show("Meow! I'm Sunny the Cat!", pictureBox1, pictureBox1.Width / 2, 0, 2000);
            PlayMeowSound();
            this.SetAnimation(CatAnimation.Happy, playOnce: true);
        }
    }
}
