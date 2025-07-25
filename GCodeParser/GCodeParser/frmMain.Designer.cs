namespace GCodeParser
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      button1 = new Button();
      button2 = new Button();
      button3 = new Button();
      button4 = new Button();
      btnConvertAProgram = new Button();
      button5 = new Button();
      btnSparCorner = new Button();
      btnRotXBasedOnYZ = new Button();
      button6 = new Button();
      SuspendLayout();
      // 
      // button1
      // 
      button1.Location = new Point(207, 39);
      button1.Name = "button1";
      button1.Size = new Size(188, 23);
      button1.TabIndex = 0;
      button1.Text = "button1";
      button1.UseVisualStyleBackColor = true;
      button1.Click += button1_Click;
      // 
      // button2
      // 
      button2.Location = new Point(207, 68);
      button2.Name = "button2";
      button2.Size = new Size(188, 23);
      button2.TabIndex = 1;
      button2.Text = "button2";
      button2.UseVisualStyleBackColor = true;
      button2.Click += btnCreateTransits;
      // 
      // button3
      // 
      button3.Location = new Point(207, 97);
      button3.Name = "button3";
      button3.Size = new Size(188, 23);
      button3.TabIndex = 2;
      button3.Text = "button3";
      button3.UseVisualStyleBackColor = true;
      button3.Click += button3_Click;
      // 
      // button4
      // 
      button4.Location = new Point(207, 126);
      button4.Name = "button4";
      button4.Size = new Size(188, 23);
      button4.TabIndex = 3;
      button4.Text = "Convert Some Positions";
      button4.UseVisualStyleBackColor = true;
      button4.Click += btn_TestTransforms;
      // 
      // btnConvertAProgram
      // 
      btnConvertAProgram.Location = new Point(207, 155);
      btnConvertAProgram.Name = "btnConvertAProgram";
      btnConvertAProgram.Size = new Size(188, 23);
      btnConvertAProgram.TabIndex = 4;
      btnConvertAProgram.Text = "Convert A Program";
      btnConvertAProgram.UseVisualStyleBackColor = true;
      btnConvertAProgram.Click += btnConvertAProgram_Click;
      // 
      // button5
      // 
      button5.Location = new Point(207, 184);
      button5.Name = "button5";
      button5.Size = new Size(188, 23);
      button5.TabIndex = 5;
      button5.Text = "Extract APPROACH_ROTX";
      button5.UseVisualStyleBackColor = true;
      button5.Click += button5_Click;
      // 
      // btnSparCorner
      // 
      btnSparCorner.Location = new Point(207, 213);
      btnSparCorner.Name = "btnSparCorner";
      btnSparCorner.Size = new Size(188, 23);
      btnSparCorner.TabIndex = 6;
      btnSparCorner.Text = "Spar Corner Treaatment";
      btnSparCorner.UseVisualStyleBackColor = true;
      btnSparCorner.Click += btnSparCorner_Click;
      // 
      // btnRotXBasedOnYZ
      // 
      btnRotXBasedOnYZ.Location = new Point(207, 242);
      btnRotXBasedOnYZ.Name = "btnRotXBasedOnYZ";
      btnRotXBasedOnYZ.Size = new Size(188, 23);
      btnRotXBasedOnYZ.TabIndex = 7;
      btnRotXBasedOnYZ.Text = "RotX ATAN(YZ)";
      btnRotXBasedOnYZ.UseVisualStyleBackColor = true;
      btnRotXBasedOnYZ.Click += btnRotXBasedOnYZ_Click;
      // 
      // button6
      // 
      button6.Location = new Point(207, 271);
      button6.Name = "button6";
      button6.Size = new Size(188, 23);
      button6.TabIndex = 8;
      button6.Text = "RotX Strategies";
      button6.UseVisualStyleBackColor = true;
      button6.Click += button6_Click;
      // 
      // frmMain
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(800, 450);
      Controls.Add(button6);
      Controls.Add(btnRotXBasedOnYZ);
      Controls.Add(btnSparCorner);
      Controls.Add(button5);
      Controls.Add(btnConvertAProgram);
      Controls.Add(button4);
      Controls.Add(button3);
      Controls.Add(button2);
      Controls.Add(button1);
      Name = "frmMain";
      Text = "Form1";
      Load += frmMain_Load;
      ResumeLayout(false);
    }

    #endregion

    private Button button1;
    private Button button2;
    private Button button3;
    private Button button4;
    private Button btnConvertAProgram;
    private Button button5;
    private Button btnSparCorner;
    private Button btnRotXBasedOnYZ;
    private Button button6;
  }
}
