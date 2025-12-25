using System;
using System.Windows.Forms;
using System.Drawing;

namespace RobloxExecutor
{
    public partial class RobloxExecutor : Form
    {
        public RobloxExecutor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "RobloxExecutor";
            this.Text = "Roblox Executor";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Icon = SystemIcons.Application;

            // Create main controls
            CreateUIControls();
        }

        private void CreateUIControls()
        {
            // Script Editor Label
            Label scriptLabel = new Label();
            scriptLabel.Text = "Script Editor:";
            scriptLabel.Location = new Point(10, 10);
            scriptLabel.Size = new Size(100, 20);
            this.Controls.Add(scriptLabel);

            // Script TextBox
            TextBox scriptTextBox = new TextBox();
            scriptTextBox.Name = "scriptTextBox";
            scriptTextBox.Location = new Point(10, 30);
            scriptTextBox.Size = new Size(760, 400);
            scriptTextBox.Multiline = true;
            scriptTextBox.ScrollBars = ScrollBars.Both;
            scriptTextBox.WordWrap = false;
            scriptTextBox.Font = new Font("Courier New", 10);
            this.Controls.Add(scriptTextBox);

            // Execute Button
            Button executeButton = new Button();
            executeButton.Name = "executeButton";
            executeButton.Text = "Execute Script";
            executeButton.Location = new Point(10, 440);
            executeButton.Size = new Size(100, 30);
            executeButton.Click += ExecuteButton_Click;
            this.Controls.Add(executeButton);

            // Clear Button
            Button clearButton = new Button();
            clearButton.Name = "clearButton";
            clearButton.Text = "Clear Script";
            clearButton.Location = new Point(120, 440);
            clearButton.Size = new Size(100, 30);
            clearButton.Click += ClearButton_Click;
            this.Controls.Add(clearButton);

            // Output Label
            Label outputLabel = new Label();
            outputLabel.Text = "Output:";
            outputLabel.Location = new Point(10, 480);
            outputLabel.Size = new Size(100, 20);
            this.Controls.Add(outputLabel);

            // Output TextBox
            TextBox outputTextBox = new TextBox();
            outputTextBox.Name = "outputTextBox";
            outputTextBox.Location = new Point(10, 500);
            outputTextBox.Size = new Size(760, 80);
            outputTextBox.Multiline = true;
            outputTextBox.ReadOnly = true;
            outputTextBox.ScrollBars = ScrollBars.Vertical;
            outputTextBox.Font = new Font("Courier New", 9);
            this.Controls.Add(outputTextBox);
        }

        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            try
            {
                TextBox scriptTextBox = this.Controls["scriptTextBox"] as TextBox;
                TextBox outputTextBox = this.Controls["outputTextBox"] as TextBox;

                if (scriptTextBox == null || outputTextBox == null)
                    return;

                string script = scriptTextBox.Text;
                
                if (string.IsNullOrWhiteSpace(script))
                {
                    outputTextBox.Text = "Error: Script is empty!";
                    return;
                }

                // Execute script logic here
                outputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Executing script...\r\n");
                outputTextBox.AppendText($"Script length: {script.Length} characters\r\n");
                outputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Script executed successfully!\r\n");
            }
            catch (Exception ex)
            {
                TextBox outputTextBox = this.Controls["outputTextBox"] as TextBox;
                if (outputTextBox != null)
                    outputTextBox.AppendText($"Error: {ex.Message}\r\n");
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            TextBox scriptTextBox = this.Controls["scriptTextBox"] as TextBox;
            TextBox outputTextBox = this.Controls["outputTextBox"] as TextBox;

            if (scriptTextBox != null)
                scriptTextBox.Clear();

            if (outputTextBox != null)
                outputTextBox.Clear();
        }

        private System.ComponentModel.IContainer components;
    }
}
