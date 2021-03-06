﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using CrazyRestaurant.Common;
using CrazyRestaurant.Properties;

namespace CrazyRestaurant
{
    public partial class FrmMain : Form
    {

        Rectangle rectMain;
        Dictionary<string, Bitmap> dicTemplate = new Dictionary<string, Bitmap>();

        Hotkey hotKey;
        Dictionary<int, Keys> dicKeys = new Dictionary<int, Keys>();
        bool inRun = false;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            RefreshState();
            var unit = GraphicsUnit.Pixel;
            dicTemplate.Add("fish", Resources.fish.Clone(Resources.fish.GetBounds(ref unit), PixelFormat.Format8bppIndexed));
            dicTemplate.Add("banana", Resources.banana.Clone(Resources.banana.GetBounds(ref unit), PixelFormat.Format8bppIndexed));
            dicTemplate.Add("bag", Resources.bag.Clone(Resources.bag.GetBounds(ref unit), PixelFormat.Format8bppIndexed));
            dicTemplate.Add("can", Resources.can.Clone(Resources.can.GetBounds(ref unit), PixelFormat.Format8bppIndexed));
            hotKey = new Hotkey(this.Handle);
            dicKeys[hotKey.RegisterHotkey(Keys.Home, Hotkey.KeyFlags.MOD_NONE)] = Keys.Home;
            dicKeys[hotKey.RegisterHotkey(Keys.End, Hotkey.KeyFlags.MOD_NONE)] = Keys.End;
            dicKeys[hotKey.RegisterHotkey(Keys.X, Hotkey.KeyFlags.MOD_ALT)] = Keys.X;
            hotKey.OnHotkey += new HotkeyEventHandler(hotKey_OnHotkey);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (inRun)
                    {
                        SetStepState(1);
                        Ads();
                        SetStepState(2);
                        Serve();
                        SetStepState(3);
                        FishHole();
                        SetStepState(4);
                        PickFishAndRubbish();
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        void RefreshState()
        {
            if (inRun)
            {
                this.labState.Image = Resources.Run;
                this.labState.Text = "Running...";
            }
            else
            {
                this.labState.Image = Resources.Stop;
                this.labState.Text = "Waiting for start...";
                SetStepState(0);
            }
        }

        void SetStepState(int step)
        {
            this.Invoke(new Action(() =>
            {
                foreach (Label lab in this.plStep.Controls)
                {
                    if (Convert.ToInt32(lab.Tag) == step)
                        lab.BackColor = Color.Gray;
                    else
                        lab.BackColor = Color.Transparent;
                }
            }));
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            hotKey.UnregisterHotkeys();
        }

        void hotKey_OnHotkey(int hotKeyID)
        {
            switch (dicKeys[hotKeyID])
            {
                case Keys.Home:
                    {
                        MouseInvoke.InitApp(AppInvoke.Init());
                        if (AppInvoke.Rectangle == Rectangle.Empty)
                            break;
                        rectMain = AppInvoke.Rectangle;
                        this.picModify.Height = rectMain.Height * this.picModify.Width / rectMain.Width;
                        inRun = true;
                        RefreshState();
                    }
                    break;
                case Keys.End:
                    {
                        inRun = false;
                        RefreshState();
                    }
                    break;
                case Keys.X:
                    {
                        this.labPos.Text = $"{MousePosition.X - rectMain.X},{ MousePosition.Y - rectMain.Y }";
                    }
                    break;
            }
        }

        private void MoveAndClick(int x, int y, int r = 5)
        {
            x += new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(-1 * r, r);
            y += new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(-1 * r, r);
            MouseInvoke.AppClick(x, y);
        }

        private void Ads()
        {
            for (int j = 0; j < 32; j++)
            {
                MoveAndClick(350, 648, 10);
                Thread.Sleep(220);
            }
        }

        private void Serve()
        {
            MoveAndClick(147, 261);
            MoveAndClick(237, 262);
            MoveAndClick(326, 261);
            MoveAndClick(151, 378);
            MoveAndClick(236, 377);
            MoveAndClick(326, 376);
        }

        private void FishHole()
        {
            MoveAndClick(58, 285);
            MoveAndClick(253, 309);
            MoveAndClick(176, 520);
            MoveAndClick(168, 593);
            MoveAndClick(162, 311);
            MoveAndClick(341, 425);
            //Tired
            MoveAndClick(197, 444);
            MoveAndClick(197, 444);
        }

        private void PickFishAndRubbish()
        {
            var bmp = new Bitmap(AppInvoke.GetImage());
            var bmpShow = bmp.Clone() as Image;
            var g = Graphics.FromImage(bmpShow);
            //灰度化
            bmp = Grayscale.CommonAlgorithms.BT709.Apply(bmp);
            //反色
            new Invert().ApplyInPlace(bmp);
            //二值化
            new Threshold(120).ApplyInPlace(bmp);
            //保存模板以用于识别
            //bmp.Save("template.bmp");
            //fish
            var matchArray = new AForge.Imaging.ExhaustiveTemplateMatching(0.8f).ProcessImage(bmp, dicTemplate["fish"]);
            foreach (var m in matchArray)
            {
                g.FillRectangle(Brushes.Red, m.Rectangle);
                MoveAndClick(m.Rectangle.X + m.Rectangle.Width / 2, m.Rectangle.Y + m.Rectangle.Height / 2, 0);
            }
            //Rubbish
            matchArray = new AForge.Imaging.ExhaustiveTemplateMatching(0.88f).ProcessImage(bmp, dicTemplate["banana"]);
            foreach (var m in matchArray)
            {
                g.FillRectangle(Brushes.Yellow, m.Rectangle);
                MoveAndClick(m.Rectangle.X + m.Rectangle.Width / 2, m.Rectangle.Y + m.Rectangle.Height / 2, 0);
            }
            matchArray = new AForge.Imaging.ExhaustiveTemplateMatching(0.88f).ProcessImage(bmp, dicTemplate["bag"]);
            foreach (var m in matchArray)
            {
                g.FillRectangle(Brushes.Green, m.Rectangle);
                MoveAndClick(m.Rectangle.X + m.Rectangle.Width / 2, m.Rectangle.Y + m.Rectangle.Height / 2, 0);
            }
            matchArray = new AForge.Imaging.ExhaustiveTemplateMatching(0.88f).ProcessImage(bmp, dicTemplate["can"]);
            foreach (var m in matchArray)
            {
                g.FillRectangle(Brushes.Green, m.Rectangle);
                MoveAndClick(m.Rectangle.X + m.Rectangle.Width / 2, m.Rectangle.Y + m.Rectangle.Height / 2, 0);
            }
            this.Invoke(new Action(() =>
            {
                this.picModify.Image = bmpShow;
            }));
        }
    }
}

