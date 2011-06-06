﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
    public partial class NESNameTableViewer : Form
    {
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        NES Nes;

        public NESNameTableViewer()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
        }

        private void SaveConfigSettings()
        {
            Global.Config.NESNameTableWndx = this.Location.X;
            Global.Config.NESNameTableWndy = this.Location.Y;
        }

        public unsafe void UpdateValues()
        {
            if (!(Global.Emulator is NES)) return;
            if (!this.IsHandleCreated || this.IsDisposed) return;
			NES.PPU ppu = (Global.Emulator as NES).ppu;

			BitmapData bmpdata = NameTableView.nametables.LockBits(new Rectangle(0, 0, 512, 480), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int* dptr = (int*)bmpdata.Scan0.ToPointer();
			int pitch = bmpdata.Stride / 4;
			int pt_add = ppu.reg_2000.bg_pattern_hi ? 0x1000 : 0;

			//TODO - buffer all the data from the ppu, because it will be read multiple times and that is slow

			int ytable = 0, yline=0;
			for (int y = 0; y < 480; y++)
			{
				if (y == 240)
				{
					ytable += 2;
					yline = 240;
				}
				for (int x = 0; x < 512; x++, dptr++)
				{
					int table = (x >> 8) + ytable;
					int ntaddr = (table << 10);
					int px = x & 255;
					int py = y - yline;
					int tx = px>>3;
					int ty = py>>3;
					int ntbyte_ptr = ntaddr + (ty * 32) + tx;
					int atbyte_ptr = ntaddr + 0x3C0 + ((ty >> 2) << 3) + (tx >> 2);
					int nt = ppu.ppubus_peek(ntbyte_ptr + 0x2000);
					
					int at = ppu.ppubus_peek(atbyte_ptr + 0x2000);
					if((ty&1)!=0) at >>= 4;
					if((tx&1)!=0) at >>= 2;
					at &= 0x03;
					at <<= 2;

					int bgpx = x & 7;
					int bgpy = y & 7;
					int pt_addr = (nt << 4) + bgpy + pt_add;
					int pt_0 = ppu.ppubus_peek(pt_addr);
					int pt_1 = ppu.ppubus_peek(pt_addr + 8);
					int pixel = ((pt_0 >> (7 - bgpx)) & 1) | (((pt_1 >> (7 - bgpx)) & 1) << 1);
					pixel |= at;

					pixel = ppu.PALRAM[pixel];
					int cvalue = Nes.LookupColor(pixel);
					*dptr = cvalue;
				}
				dptr += pitch - 512;
			}

			NameTableView.nametables.UnlockBits(bmpdata);

            NameTableView.Refresh();
        }

        public void Restart()
        {
            if (!(Global.Emulator is NES)) this.Close();
            if (!this.IsHandleCreated || this.IsDisposed) return;
            Nes = Global.Emulator as NES;
        }

        private void NESNameTableViewer_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            if (Global.Config.NESNameTableSaveWindowPosition && Global.Config.NESNameTableWndx >= 0 && Global.Config.NESNameTableWndy >= 0)
                this.Location = new Point(Global.Config.NESNameTableWndx, Global.Config.NESNameTableWndy);

            Nes = Global.Emulator as NES;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.AutoLoadNESNameTable ^= true;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.NESNameTableSaveWindowPosition ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESNameTable;
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESNameTableSaveWindowPosition;
        }
    }
}
