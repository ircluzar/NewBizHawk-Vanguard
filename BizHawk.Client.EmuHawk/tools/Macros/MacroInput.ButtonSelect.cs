﻿using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MacroInputTool
	{
		private CheckBox[] _buttonBoxes;
		private void SetUpButtonBoxes()
		{
			ControllerDefinition def = Global.Emulator.ControllerDefinition;
			int count = def.BoolButtons.Count + def.FloatControls.Count;
			_buttonBoxes = new CheckBox[count];

			for (int i = 0; i < def.FloatControls.Count; i++)
			{
				CheckBox box = new CheckBox {Text = def.FloatControls[i]};
				_buttonBoxes[i] = box;
			}
			for (int i = 0; i < def.BoolButtons.Count; i++)
			{
				CheckBox box = new CheckBox {Text = def.BoolButtons[i]};
				_buttonBoxes[i + def.FloatControls.Count] = box;
			}

			foreach (var box in _buttonBoxes)
			{
				box.Parent = this;
				box.AutoSize = true;
				box.Checked = true;
				box.CheckedChanged += ButtonBox_CheckedChanged;
			}

			PositionBoxes();
		}

		private void ButtonBox_CheckedChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null)
			{
				return;
			}

			CheckBox s = (CheckBox)sender;
			s.ForeColor = s.Checked ? SystemColors.ControlText : SystemColors.ButtonShadow;
			s.Refresh();

			// Update the selected zone's key
			var lg = (Bk2LogEntryGenerator)Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			string key = lg.GenerateLogKey();
			key = key.Replace("LogKey:", "").Replace("#", "");

			foreach (var box in _buttonBoxes)
			{
				if (!box.Checked)
				{
					key = key.Replace($"{box.Text}|", "");
				}
			}

			key = key.Substring(0, key.Length - 1);

			SelectedZone.InputKey = key;
		}

		private void PositionBoxes()
		{
			int x = ClientSize.Width - 3;
			int y = ClientSize.Height - _buttonBoxes[0].Height - 3;

			for (int i = _buttonBoxes.Length - 1; i >= 0; i--)
			{
				x -= _buttonBoxes[i].Width;
				if (x <= 3)
				{
					x = ClientSize.Width - 3 - _buttonBoxes[i].Width;
					y -= (_buttonBoxes[0].Height + 6);
				}

				_buttonBoxes[i].Location = new Point(x, y);
			}
		}
		private void MacroInputTool_Resize(object sender, EventArgs e)
		{
			if (_initializing)
			{
				return;
			}

			PositionBoxes();
		}
	}
}