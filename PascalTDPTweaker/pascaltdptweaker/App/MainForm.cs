﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;


namespace PascalTDPTweaker
{
    public partial class MainForm : Form
    {
        Models.Form.IndexCollector ic;
        readonly Models.Bios.Reader reader = new Models.Bios.Reader();
        private int globalCounter;
        private bool textChanging = false;
        private int type;
        private byte[] bios;
        private long checksumValue = 0;
        private string checksum8bit;
        private readonly string FORMAT = "ddd, MMM d, yyyy HH:mm:ss";

        // Indices for vBIOS information.
        private readonly int NUMADDR = 23; // Total number of indices.
        private readonly int SAFE_LENGTH = 70; // Maximum length to use for correcting checksum.

        public MainForm()
        {
            InitializeComponent();
        }

        // Load vBIOS file into byte array. 
        // Loop through byte by byte to locate predefined addresses and set values in form.
        private void OpenBIOS_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Title = "Open BIOS";
            open.Filter = "BIOS Files (*.rom; *.bin)|*.rom;*.bin";
            // Default value for mobile card: mobile = 1; desktop = 2.
            type = 1;
            
            if (open.ShowDialog() == DialogResult.OK)
            {
                // Load models config file.
                reader.ReadConfigFile("PascalTDPTweaker.Models.config", "Generic");

                InitForm();
                ic = new Models.Form.IndexCollector();
                textBox6.Text = open.SafeFileName;
                DateTime lastModified = File.GetLastWriteTime(open.FileName);
                textBox3.Text = lastModified.ToString(FORMAT);
                bios = File.ReadAllBytes(open.FileName);
                open.Dispose();

                checksumValue = CalculateChecksum(bios);
                globalCounter = 0;

                // Sequentially read bytes; compare bytes read with addresses in 'Models.config'.
                // If address matched, set index and display value in form.
                for (int i = 0; i < bios.Length; i++)
                {
                    // Break for-loop once all indices are set.
                    if (globalCounter == NUMADDR) break;

                    // Index to determine if black screen fix is needed.
                    ic.BlackSreenIndex = SetValueIndex(reader.StartAdr.ID, bios, i, reader.StartAdr.Low, reader.StartAdr.High, null, 0, ic.BlackSreenIndex, 1);

                    // vBIOS information section. //
                    ic.DateIndex = SetTextIndex(reader.DateAdr.ID, bios, i, reader.DateAdr.Low, reader.DateAdr.High, textBox10, 1, ic.DateIndex, 0);
                    ic.NameIndex = SetTextIndex(reader.NameAdr.ID, bios, i, reader.NameAdr.Low, reader.NameAdr.High, textBox8, 1, ic.NameIndex, 1);
                    ic.VersionIndex = SetTextIndex(reader.VerAdr.ID, bios, i, reader.VerAdr.Low, reader.VerAdr.High, textBox9, 1, ic.VersionIndex, 0);
                    ic.BoardIndex = SetTextIndex(reader.BoardAdr.ID, bios, i, reader.BoardAdr.Low, reader.BoardAdr.High, textBox7, 1, ic.BoardIndex, 0);
                    // End information section.

                    // To be modded. //
                    // TDP section. //
                    // Negative value to shift forward.
                    ic.TdpSliderIndex = SetRadioIndex(reader.TDPSliderAdr.ID, bios, i, reader.TDPSliderAdr.Low, reader.TDPSliderAdr.High, tdpAdjustable, tdpFixed, reader.TDPSliderSignal.Code, ic.TdpSliderIndex);
                    ic.BaseTdpIndex = SetValueIndex(reader.BaseTDPAdr.ID, bios, i, reader.BaseTDPAdr.Low, reader.BaseTDPAdr.High, numericUpDown1, 1, ic.BaseTdpIndex, 0);
                    ic.MaxTdpIndex = SetValueIndex(reader.MaxTDPAdr.ID, bios, i, reader.MaxTDPAdr.Low, reader.MaxTDPAdr.High, numericUpDown2, 1, ic.MaxTdpIndex, 0);
                    // End TDP section.

                    // Extreme power section. //
                    // 48 3F 00 00. (Mobile 1080 = 16200)
                    ic.P1BaseTdpIndex = SetValueIndex(reader.Power1BaseAdr.ID, bios, i, reader.Power1BaseAdr.Low, reader.Power1BaseAdr.High, numericUpDown7, 1, ic.P1BaseTdpIndex, 0);
                    ic.P1MaxTdpIndex = SetValueIndex(reader.Power1MaxAdr.ID, bios, i, reader.Power1MaxAdr.Low, reader.Power1MaxAdr.High, numericUpDown8, 1, ic.P1MaxTdpIndex, 0);

                    // 50 B1 03 00. (Mobile 1080 = 242000)
                    ic.P2BaseTdpIndex = SetValueIndex(reader.Power2BaseAdr.ID, bios, i, reader.Power2BaseAdr.Low, reader.Power2BaseAdr.High, numericUpDown9, 1, ic.P2BaseTdpIndex, 0);
                    ic.P2MaxTdpIndex = SetValueIndex(reader.Power2MaxAdr.ID, bios, i, reader.Power2MaxAdr.Low, reader.Power2MaxAdr.High, numericUpDown10, 1, ic.P2MaxTdpIndex, 0);

                    // 80 19 02 00. (Mobile 1080 = 137600)
                    ic.P3BaseTdpIndex = SetValueIndex(reader.Power3BaseAdr.ID, bios, i, reader.Power3BaseAdr.Low, reader.Power3BaseAdr.High, numericUpDown11, 1, ic.P3BaseTdpIndex, 0);
                    ic.P3MaxTdpIndex = SetValueIndex(reader.Power3MaxAdr.ID, bios, i, reader.Power3MaxAdr.Low, reader.Power3MaxAdr.High, numericUpDown12, 1, ic.P3MaxTdpIndex, 0);
                    // End extreme power section.

                    // Temperature section. //   
                    // Positive value to shift backward, then negative to shift forward.
                    ic.TempSliderIndex = SetRadioIndex(reader.TempSliderAdr.ID, bios, i, reader.TempSliderAdr.Low, reader.TempSliderAdr.High, tempAdjustable, tempFixed, reader.TempSliderSignal.Code, ic.TempSliderIndex); 
                    ic.T1BaseTempIndex = SetValueIndex(reader.Temp1BaseAdr.ID, bios, i, reader.Temp1BaseAdr.Low, reader.Temp1BaseAdr.High, numericUpDown3, 2, ic.T1BaseTempIndex, 0);
                    ic.T1MaxTempIndex = SetValueIndex(reader.Temp1MaxAdr.ID, bios, i, reader.Temp1MaxAdr.Low, reader.Temp1MaxAdr.High, numericUpDown4, 2, ic.T1MaxTempIndex, 0);
                    ic.T1MinTempIndex = SetValueIndex(reader.Temp1MinAdr.ID, bios, i, reader.Temp1MinAdr.Low, reader.Temp1MinAdr.High, numericUpDown5, 2, ic.T1MinTempIndex, 0);

                    // Additional Temperature settings.
                    ic.T2BaseTempIndex = SetValueIndex(reader.Temp2BaseAdr.ID, bios, i, reader.Temp2BaseAdr.Low, reader.Temp2BaseAdr.High, numericUpDown13, 2, ic.T2BaseTempIndex, 0);
                    ic.T2MaxTempIndex = SetValueIndex(reader.Temp2MaxAdr.ID, bios, i, reader.Temp2MaxAdr.Low, reader.Temp2MaxAdr.High, numericUpDown14, 2, ic.T2MaxTempIndex, 0);
                    ic.T2MinTempIndex = SetValueIndex(reader.Temp2MinAdr.ID, bios, i, reader.Temp2MinAdr.Low, reader.Temp2MinAdr.High, numericUpDown15, 2, ic.T2MinTempIndex, 0);
                    // End Temperature section.

                    // Checksum index.
                    ic.CheckSum32Index = SetValueIndex(reader.Checksum32Adr.ID, bios, i, reader.Checksum32Adr.Low, reader.Checksum32Adr.High, null, 0, ic.CheckSum32Index, 0);
                    ic.CheckSum8Index = SetTextIndex(reader.Checksum8Adr.ID, bios, i, reader.Checksum8Adr.Low, reader.Checksum8Adr.High, textBox15, 2, ic.CheckSum8Index, 2);

                    // Set unknown if date index cannot be found.
                    if (ic.DateIndex == -1) textBox10.Text = "Unknown";
                }
            }
        }

        // Perform extra actions for some values.
        private void DoExtra(int index, int extra)
        {
            switch (extra)
            {
                case 1:
                    DisplayModel(index, this.textBox2, this.textBox8);
                    break;
                case 2:
                    this.checksum8bit = this.textBox15.Text;
                    Is8bitCorrect();
                    break;
                default:
                    break;
            }
        }

        // Display model name.
        private void DisplayModel(int nameIndex, TextBox tb1, TextBox tb2)
        {
            if (nameIndex != -1)
            {
                string name = tb2.Text;
                name = name.Contains("!") ? name.Substring(1) : name;
                string model = reader.ReadModel(name);
                tb2.Text = name;
                tb1.Text = model;

                if (model == "Unsupported")
                {
                    tb1.BackColor = Color.AntiqueWhite;
                    tb2.BackColor = Color.AntiqueWhite;
                    tb2.Text = model;
                }
                if (!model.Contains("Notebook"))
                {
                    // Temporarily disable support for desktop.
                    //this.type = 2;
                }
            }
        }

        // Wrapper method calls SetText and return address index.
        private int SetTextIndex(byte[] address, byte[] vbios, int pos, int dataLen, int shift, TextBox tb, int type, int index, int extra)
        {
            if (index == -1)
            {
                index = SetText(address, vbios, pos, dataLen, shift, tb, type);

                if (index != -1)
                {
                    globalCounter++;
                    DoExtra(index, extra);
                    return index;
                }
            }
            return index;
        }

        // Wrapper method calls SetValue and return address index.
        private int SetValueIndex(byte[] address, byte[] vbios, int pos, int dataLen, int shift, NumericUpDown nud, int type, int index, int extra)
        {
            if (index == -1)
            {
                index = SetValue(address, vbios, pos, dataLen, shift, nud, type);
                if (index != -1)
                {
                    globalCounter++;
                    if (extra == 1)
                    {
                        index -= 2;
                        ShowTB11TB12();
                    }
                    return index;
                }
            }
            return index;
        }

        // Wrapper method calls SetRadio and return address index.
        private int SetRadioIndex(byte[] address, byte[] vbios, int pos, int dataLen, int shift, RadioButton rb1, RadioButton rb2, string signal, int index)
        {
            if (index == -1)
            {
                index = SetRadio(address, vbios, pos, dataLen, shift, rb1, rb2, signal);
                if (index != -1)
                {
                    globalCounter++;
                    return index;
                }
            }
            return index;
        }

        // Save vBIOS file. 
        private void SaveBIOS_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Title = "Save BIOS";
            save.Filter = "BIOS Files (*.rom; *.bin)|*.rom;*.bin";

            if (ic.CheckSum32Index == 0)
                // No vBIOS file is opened.
                MessageBox.Show("Unhandled exception (empty)! BIOS cannot be saved.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error); 
            else if (ic.CheckSum32Index == -1)
                // Cannot locate "Power" String.
                MessageBox.Show("Unhandled exception (corrupted string section)! BIOS cannot be saved.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (ic.BaseTdpIndex != -1 && ic.MaxTdpIndex != -1)
                {
                    ModBIOSValue(bios, ic.BaseTdpIndex, (int)numericUpDown1.Value, 1);
                    ModBIOSValue(bios, ic.MaxTdpIndex, (int)numericUpDown2.Value, 1);
                }
                if (ic.T1BaseTempIndex != -1 && ic.T1MaxTempIndex != -1 && ic.T1MinTempIndex != -1)
                {
                    ModBIOSValue(bios, ic.T1BaseTempIndex, (int)numericUpDown3.Value, 2);
                    ModBIOSValue(bios, ic.T1MaxTempIndex, (int)numericUpDown4.Value, 2);
                    ModBIOSValue(bios, ic.T1MinTempIndex, (int)numericUpDown5.Value, 2);
                }
                if (ic.T2BaseTempIndex != -1 && ic.T2MaxTempIndex != -1 && ic.T2MinTempIndex != -1)
                {
                    ModBIOSValue(bios, ic.T2BaseTempIndex, (int)numericUpDown13.Value, 2);
                    ModBIOSValue(bios, ic.T2MaxTempIndex, (int)numericUpDown14.Value, 2);
                    ModBIOSValue(bios, ic.T2MinTempIndex, (int)numericUpDown15.Value, 2);
                }
                if (ic.TdpSliderIndex != -1) ModBIOSSlide(bios, ic.TdpSliderIndex, tdpAdjustable.Checked, tdpFixed.Checked, reader.TDPSliderSignal.Code);
                if (ic.TempSliderIndex != -1) ModBIOSSlide(bios, ic.TempSliderIndex, tempAdjustable.Checked, tempFixed.Checked, reader.TempSliderSignal.Code);
                if (ic.P1BaseTdpIndex != -1 && ic.P1MaxTdpIndex != -1)
                {
                    ModBIOSValue(bios, ic.P1BaseTdpIndex, (int)numericUpDown7.Value, 1);
                    ModBIOSValue(bios, ic.P1MaxTdpIndex, (int)numericUpDown8.Value, 1);
                }
                if (ic.P2BaseTdpIndex != -1 && ic.P2MaxTdpIndex  != -1)
                {
                    ModBIOSValue(bios, ic.P2BaseTdpIndex, (int)numericUpDown9.Value, 1);
                    ModBIOSValue(bios, ic.P2MaxTdpIndex , (int)numericUpDown10.Value, 1);
                }
                if (ic.P3BaseTdpIndex != -1 && ic.P3MaxTdpIndex != -1)
                {
                    ModBIOSValue(bios, ic.P3BaseTdpIndex, (int)numericUpDown11.Value, 1);
                    ModBIOSValue(bios, ic.P3MaxTdpIndex, (int)numericUpDown12.Value, 1);
                }
                long newCS = CalculateChecksum(bios);
                long offset = newCS - checksumValue;

                //Console.WriteLine(offset);
                if (CorrectCS(offset, 1) == 1)
                {
                    // Fix non-gysn vBIOS that has header.
                    //BlackScreenFix(); 
                    File.WriteAllBytes(save.FileName, bios);
                }    
                else
                    MessageBox.Show("Unhandled exception (corrupted string section)! BIOS cannot be saved.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Load modded preset values for major mobile models.
        private void Preset_Click(object sender, EventArgs e)
        {
            string model = textBox2.Text;
            try
            {
                numericUpDown1.Value = reader.AssignPresetValues(model, 1);
                numericUpDown2.Value = reader.AssignPresetValues(model, 2);
                numericUpDown7.Value = reader.AssignPresetValues(model, 7);
                numericUpDown8.Value = reader.AssignPresetValues(model, 8);
                numericUpDown9.Value = reader.AssignPresetValues(model, 9);
                numericUpDown10.Value = reader.AssignPresetValues(model, 10);
                numericUpDown11.Value = reader.AssignPresetValues(model, 11);
                numericUpDown12.Value = reader.AssignPresetValues(model, 12);
                numericUpDown3.Value = numericUpDown4.Value = numericUpDown5.Value = reader.AssignPresetValues(model, 5);
                numericUpDown13.Value = numericUpDown14.Value = numericUpDown15.Value = reader.AssignPresetValues(model, 5);

                tdpAdjustable.Checked = true;
                tdpFixed.Checked = false;
            }
            catch (Exception)
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show("Preset is not supported for this model.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }          
        }

        // Call CorrectCS to correct checksum. Standalone checksum correction.
        private void QuickFix_Click(object sender, EventArgs e)
        {
            if (ic.CheckSum32Index == -1)
                MessageBox.Show("Unhandled exception (corrupted string section)! Checksum cannot be corrected.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (ic.CheckSum32Index != 0)
            {
                long fixCS = (long)numericUpDown6.Value;
                long offset = checksumValue - fixCS;
                Console.WriteLine(offset);
                if (offset == 0)
                    MessageBox.Show("Checksum offset is 0. No checksum correction needed.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (CorrectCS(offset, 2) == 1)
                {
                    MessageBox.Show("Offset " + offset + ". Checksum has been corrected.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    checksumValue = fixCS;
                }
                else
                    MessageBox.Show("Unhandled exception (corrupted section/offset too large)! Checksum cannot be corrected.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Fix black screen issue for some older bios.
        private int BlackScreenFix()
        {
            // Only fix mobile cards == 1.
            if (type == 1 && ic.BlackSreenIndex != 0)
            {
                byte[] new_bios = new byte[bios.Length-ic.BlackSreenIndex]; // For backup.
                int j = ic.BlackSreenIndex;

                for (int i = 0; i < new_bios.Length; i++)
                {
                    new_bios[i] = bios[j];
                    j++;
                }
                bios = new_bios;
                return 1;
            }
            return -1;
        }

        // Correct checksum based on offset.
        private int CorrectCS(long offset, int mode)
        {
            long fixCS = (long)numericUpDown6.Value;
            int range = ic.CheckSum32Index + SAFE_LENGTH;
            long rest = offset;

            // For backup.
            byte[] bp_bios = new byte[bios.Length];

            for (int i = 0; i < bios.Length; i++)
            {
                bp_bios[i] = bios[i];
            }

            for (int i = ic.CheckSum32Index; i < range; i++)
            {
                int cur = bios[i];
                if (rest >= cur)
                {
                    rest = rest - cur;
                    bios[i] = (byte)00;
                }
                else if (rest > 0 && rest < cur)
                {
                    bios[i] = (byte)(cur - rest);
                    rest = 0;
                }
                else if (rest < 0)
                {
                    int spare = 255 - cur;
                    if (-rest >= spare)
                    {
                        rest = rest + spare;
                        bios[i] = (byte)255;
                    }
                    else if (-rest < spare)
                    {
                        bios[i] = (byte)(-rest + cur);
                        rest = 0;
                    }
                }
                if (rest == 0)
                    break;
            }

            long CS = CalculateChecksum(bios);
            // For regular modding: mode = 1
            if (mode == 1 && CS == checksumValue)
            {
                return 1;
            }
            // Checksum fix: mode = 2
            else if (mode == 2 && CS == fixCS)
            {
                return 1;
            }
            else
            {
                SetChecksum(checksumValue);
                bios = bp_bios;
                return -1;
            }
        }

        // Modify vBIOS values in place.
        private void ModBIOSValue(byte[] file, int index, int decValue, int type)
        {
            string hexValue = "";
            // TDP = 1.
            if (type == 1)
            {
                //Console.WriteLine(decValue);
                hexValue = decValue.ToString("X2");
                if (hexValue.Length < 8)
                {
                    hexValue = "00000000" + hexValue;
                    hexValue = hexValue.Substring(hexValue.Length - 8);
                    //Console.WriteLine(hexValue);
                }
                byte[] data = Models.Helper.HexToDecString(Models.Helper.ReverseTDP(hexValue));
                int j = index;
                for (int i = 0; i < data.Length; i++)
                {
                    bios[j] = data[i];
                    j++;
                }
            }
            // Temperature = 2.
            else if (type == 2)
            {
                //Console.WriteLine(decValue);
                hexValue = (decValue*32).ToString("X2");
                if (hexValue.Length < 4)
                {
                    hexValue = "0000" + hexValue;
                    hexValue = hexValue.Substring(hexValue.Length - 4);
                    //Console.WriteLine(hexValue);
                }
                byte[] data = Models.Helper.HexToDecString(Models.Helper.ReverseTemp(hexValue));
                int j = index;
                for (int i = 0; i < data.Length; i++)
                {
                    bios[j] = data[i];
                    j++;
                }
            }
        }

        // Modify vBIOS slider in place.
        private void ModBIOSSlide(byte[] file, int index, bool adj, bool fix, string signal)
        {
            if (adj == true && fix == false)
            {
                byte[] data = Models.Helper.HexToDecString(signal.Substring(0, 8));
                int j = index;
                for (int i = 0; i < data.Length; i++)
                {
                    bios[j] = data[i];
                    j++;
                }
            }
            else if (fix == true && adj == false)
            {
                byte[] data = Models.Helper.HexToDecString(signal.Substring(8, 8));
                int j = index;
                for (int i = 0; i < data.Length; i++)
                {
                    bios[j] = data[i];
                    j++;
                }
            }
        }

        // Set text on application.
        private int SetText(byte[] dataPos, byte[] file, int i, int dataLen, int shift, TextBox tb, int type)
        {
            int pos = i;
            int posLen = dataPos.Length;

            for (int j = (posLen - 1); j >= 0; j--)
            {
                if (dataPos[j].Equals(file[pos]))
                    pos--;
                else
                    return -1;

                // Indicating there is a match.
                if (pos == (i - (posLen - 1)))
                {
                    string result = "";
                    int index = pos - dataLen;

                    for (int k = index; k < (pos + shift); k++)
                    {
                        result += file[k].ToString("X2");
                    }

                    if (type == 1)
                    {
                        tb.Text = Models.Helper.HexToString(result);
                    }
                    else if (type == 2)
                        tb.Text = result;

                    return index;
                }
            }
            return -1;
        }

        // Set value on application.
        private int SetValue(byte[] dataPos, byte[] file, int i, int dataLen, int shift, NumericUpDown nud, int type)
        {
            int pos = i;
            int posLen = dataPos.Length;

            for (int j = (posLen - 1); j >= 0; j--)
            {
                if (dataPos[j].Equals(file[pos]))
                    pos--;
                else
                    return -1;

                // Indicating there is a match.
                if (pos == (i - (posLen - 1)))
                {
                    // Immediately return index to correct checksum.
                    if (type == 0)
                        return (pos + posLen);

                    string result = "";
                    int index = pos - dataLen;

                    for (int k = index; k < (pos + shift); k++)
                    {
                        result += file[k].ToString("X2");
                    }

                    // TDP = 1
                    if (type == 1)
                    {
                        int num = int.Parse(Models.Helper.ReverseTDP(result), System.Globalization.NumberStyles.HexNumber);
                        if (num <= nud.Maximum)
                            nud.Value = num;
                    }
                    // Temp = 2
                    else if (type == 2)
                    {
                        int num = int.Parse(Models.Helper.ReverseTemp(result), System.Globalization.NumberStyles.HexNumber) / 32;
                        if (num <= nud.Maximum)
                            nud.Value = num;
                    }

                    return index;
                }
            }
            return -1;
        }

        // Set radio button on application.
        private int SetRadio(byte[] dataPos, byte[] file, int i, int dataLen, int shift, RadioButton adj, RadioButton fix, string signal)
        {
            int pos = i;
            int posLen = dataPos.Length;

            for (int j = (posLen - 1); j >= 0; j--)
            {
                if (dataPos[j].Equals(file[pos]))
                    pos--;
                else
                    return -1;

                // Indicating there is a match.
                if (pos == (i - (posLen - 1)))
                {
                    string result = "";
                    int index = pos - dataLen;

                    for (int k = index; k < (pos + shift); k++)
                    {
                        result += file[k].ToString("X2");
                    }

                    if (result.Equals(signal.Substring(0, 8)))
                        adj.Checked = true;
                    else if (result.Equals(signal.Substring(8, 8)))
                        fix.Checked = true;

                    return index;
                }
            }
            return -1;
        }

        // Calculate vBIOS checksum value.
        private long CalculateChecksum(byte[] byteToCalculate)
        {
            long checksum = 0;
            foreach (byte chData in byteToCalculate)
            {
                checksum += chData;
            }
            checksum &= 0xffffffff;
            SetChecksum(checksum);
            return checksum;
        }

        // Experimental check on 8 bit checksum. Need testing to confirm if it works.
        private long Is8bitCorrect()
        {
            int headerSize = 0;
            for (int i = 0; i < ic.BlackSreenIndex; i++)
            {
                headerSize += bios[i];
            }
            long validChecksum = checksumValue - headerSize;
            string r = validChecksum.ToString("X2");
            string lastDigits = r.Substring(r.Length - 2);

            if (lastDigits.Equals("00"))
            {
                textBox15.Text = checksum8bit + "=>[" + checksum8bit + "]";
                textBox15.BackColor = System.Drawing.Color.LemonChiffon;
                return 1;
            }
            else
            {
                int ld = Convert.ToInt32(lastDigits, 16);
                int bit = Convert.ToInt32(checksum8bit, 16);
                string diff = "";
                if (ld <= bit)
                    diff = (bit - ld).ToString("X2");
                else
                    diff = (256 - ld + bit).ToString("X2");
                textBox15.Text = checksum8bit + "=>[" + diff + "]";
                textBox15.BackColor = Color.White;
                return -1;
            }    
        }

        // Set checksum value on application.
        private void SetChecksum(long checksum)
        {
            string r = checksum.ToString("X2");
            textBox1.Text = "0" + r + " (" + checksum + ")";
            numericUpDown6.Value = checksum;
            // Only check if 8 bit correct.
            if ((r.Substring(r.Length - 2)).Equals("00"))
                textBox1.BackColor = System.Drawing.Color.LemonChiffon;
            else
                textBox1.BackColor = Color.White;
        }

        // Reset values and indices when new vBIOS is open.
        private void InitForm()
        {
            bios = null;
            checksumValue = -1;

            textBox2.BackColor = Color.LightYellow;
            textBox8.BackColor = Color.LightYellow;
            textBox2.Text = "";
            textBox7.Text = "";
            textBox8.Text = "";
            textBox9.Text = "";
            textBox10.Text = "";
            numericUpDown1.Value = 0;
            numericUpDown2.Value = 0;
            numericUpDown3.Value = 0;
            numericUpDown4.Value = 0;
            numericUpDown5.Value = 0;
            numericUpDown7.Value = 0;
            numericUpDown8.Value = 0;
            numericUpDown9.Value = 0;
            numericUpDown10.Value = 0;
            numericUpDown11.Value = 0;
            numericUpDown12.Value = 0;
            numericUpDown13.Value = 0;
            numericUpDown14.Value = 0;
            numericUpDown15.Value = 0;
            tdpAdjustable.Checked = false;
            tdpFixed.Checked = false;
            tempAdjustable.Checked = false;
            tempFixed.Checked = false;
        }

        // Hex value input box.
        private void TextBox4_TextChanged(object sender, EventArgs e)
        {
            if (textChanging == false)
            {
                textChanging = true;
                string hexValue = textBox4.Text;
                if (hexValue.Length > 0)
                {
                    long decValue = Convert.ToInt64(hexValue, 16);
                    textBox5.Text = "" + decValue;
                    while (hexValue.Length < 8)
                    {
                        hexValue = "0" + hexValue;
                    }
                    textBox13.Text = Models.Helper.Spacing(hexValue);
                    textBox14.Text = Models.Helper.Spacing(Models.Helper.ReverseTDP(hexValue));
                }
                else
                {
                    textBox5.Text = "";
                    textBox13.Text = "";
                    textBox14.Text = "";
                }
                textChanging = false;
            }
        }

        // Dec value input box.
        private void TextBox5_TextChanged(object sender, EventArgs e)
        {
            if (textChanging == false)
            {
                textChanging = true;
                string strValue = textBox5.Text;
                if (strValue.Length > 0)
                {
                    long decValue = Convert.ToInt64(strValue);
                    string hexValue = decValue.ToString("X2");
                    while (hexValue.Length < 8)
                    {
                        hexValue = "0" + hexValue;
                    }
                    textBox4.Text = hexValue;
                    textBox13.Text = Models.Helper.Spacing(hexValue);
                    textBox14.Text = Models.Helper.Spacing(Models.Helper.ReverseTDP(hexValue));
                }
                else
                {
                    textBox4.Text = "";
                    textBox13.Text = "";
                    textBox14.Text = "";
                }
                textChanging = false;
            }
        }
        
        // Detect key press for input of base converter.
        private void TextBox4_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            // Check for a naughty character in the KeyDown event.
            if (System.Text.RegularExpressions.Regex.IsMatch(e.KeyChar.ToString(), @"[^0-9^+^a-f^+^A-F^+^\b^]"))
            {
                // Stop the character from being entered into the control since it is illegal.
                e.Handled = true;
                System.Media.SystemSounds.Beep.Play();
            }
        }

        // Detect key press for input of base converter.
        private void TextBox5_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            // Check for a naughty character in the KeyDown event.
            if (System.Text.RegularExpressions.Regex.IsMatch(e.KeyChar.ToString(), @"[^0-9^+^\b^]"))
            {
                // Stop the character from being entered into the control since it is illegal.
                e.Handled = true;
                System.Media.SystemSounds.Beep.Play();
            }
        }

        // Set texts for textbox 11 and 12. 
        private void ShowTB11TB12()
        {
            string address = ic.BlackSreenIndex.ToString("X2");
            while (address.Length < 8)
            {
                address = "0" + address;
            }
            textBox11.Text = address;
            textBox12.Text = "" + bios[0].ToString("X2") + bios[1].ToString("X2");
        }
    }
}