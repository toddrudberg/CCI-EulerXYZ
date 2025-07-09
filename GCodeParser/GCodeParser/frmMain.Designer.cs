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
      SuspendLayout();
      // 
      // button1
      // 
      button1.Location = new Point(503, 107);
      button1.Margin = new Padding(7, 8, 7, 8);
      button1.Name = "button1";
      button1.Size = new Size(457, 63);
      button1.TabIndex = 0;
      button1.Text = "button1";
      button1.UseVisualStyleBackColor = true;
      button1.Click += button1_Click;
      // 
      // button2
      // 
      button2.Location = new Point(503, 186);
      button2.Margin = new Padding(7, 8, 7, 8);
      button2.Name = "button2";
      button2.Size = new Size(457, 63);
      button2.TabIndex = 1;
      button2.Text = "button2";
      button2.UseVisualStyleBackColor = true;
      button2.Click += btnCreateTransits;
      // 
      // button3
      // 
      button3.Location = new Point(503, 265);
      button3.Margin = new Padding(7, 8, 7, 8);
      button3.Name = "button3";
      button3.Size = new Size(457, 63);
      button3.TabIndex = 2;
      button3.Text = "button3";
      button3.UseVisualStyleBackColor = true;
      button3.Click += button3_Click;
      // 
      // button4
      // 
      button4.Location = new Point(503, 344);
      button4.Margin = new Padding(7, 8, 7, 8);
      button4.Name = "button4";
      button4.Size = new Size(457, 63);
      button4.TabIndex = 3;
      button4.Text = "Convert Some Positions";
      button4.UseVisualStyleBackColor = true;
      button4.Click += btn_TestTransforms;
      // 
      // btnConvertAProgram
      // 
      btnConvertAProgram.Location = new Point(503, 423);
      btnConvertAProgram.Margin = new Padding(7, 8, 7, 8);
      btnConvertAProgram.Name = "btnConvertAProgram";
      btnConvertAProgram.Size = new Size(457, 63);
      btnConvertAProgram.TabIndex = 4;
      btnConvertAProgram.Text = "Convert A Program";
      btnConvertAProgram.UseVisualStyleBackColor = true;
      btnConvertAProgram.Click += btnConvertAProgram_Click;
      // 
      // button5
      // 
      button5.Location = new Point(503, 502);
      button5.Margin = new Padding(7, 8, 7, 8);
      button5.Name = "button5";
      button5.Size = new Size(457, 63);
      button5.TabIndex = 5;
      button5.Text = "Extract APPROACH_ROTX";
      button5.UseVisualStyleBackColor = true;
      button5.Click += button5_Click;
      // 
      // frmMain
      // 
      AutoScaleDimensions = new SizeF(17F, 41F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(1943, 1230);
      Controls.Add(button5);
      Controls.Add(btnConvertAProgram);
      Controls.Add(button4);
      Controls.Add(button3);
      Controls.Add(button2);
      Controls.Add(button1);
      Margin = new Padding(7, 8, 7, 8);
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
  }
}
