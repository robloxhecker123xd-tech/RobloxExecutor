using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RobloxExecutor
{
    /// <summary>
    /// Main form for the Roblox Executor application.
    /// Provides script execution, game interaction, and user interface management.
    /// </summary>
    public partial class RobloxExecutorForm : Form
    {
        private RobloxExecutorEngine _executorEngine;
        private bool _isConnected = false;
        private HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the RobloxExecutorForm class.
        /// </summary>
        public RobloxExecutorForm()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _executorEngine = new RobloxExecutorEngine();
            
            // Attach event handlers
            AttachEventHandlers();
            
            // Initialize UI
            InitializeUI();
        }

        /// <summary>
        /// Attaches event handlers to form controls.
        /// </summary>
        private void AttachEventHandlers()
        {
            this.Load += RobloxExecutorForm_Load;
            this.FormClosing += RobloxExecutorForm_FormClosing;
            
            // Button event handlers
            if (this.btnConnect != null)
                this.btnConnect.Click += BtnConnect_Click;
            
            if (this.btnExecute != null)
                this.btnExecute.Click += BtnExecute_Click;
            
            if (this.btnClear != null)
                this.btnClear.Click += BtnClear_Click;
            
            if (this.btnLoadScript != null)
                this.btnLoadScript.Click += BtnLoadScript_Click;
            
            if (this.btnSaveScript != null)
                this.btnSaveScript.Click += BtnSaveScript_Click;
        }

        /// <summary>
        /// Initializes the user interface.
        /// </summary>
        private void InitializeUI()
        {
            this.Text = "Roblox Executor";
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;

            // Create script editor
            CreateScriptEditor();
            
            // Create control panel
            CreateControlPanel();
            
            // Create status bar
            CreateStatusBar();
        }

        /// <summary>
        /// Creates the script editor controls.
        /// </summary>
        private void CreateScriptEditor()
        {
            TextBox scriptEditor = new TextBox
            {
                Name = "txtScript",
                Multiline = true,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Courier New", 10),
                AcceptsTab = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                Text = "-- Roblox Lua Script Editor\n-- Start typing your script here\n"
            };
            
            this.Controls.Add(scriptEditor);
        }

        /// <summary>
        /// Creates the control panel with buttons and status indicators.
        /// </summary>
        private void CreateControlPanel()
        {
            Panel controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = System.Drawing.SystemColors.Control,
                Padding = new Padding(10)
            };

            // Connect button
            this.btnConnect = new Button
            {
                Name = "btnConnect",
                Text = "Connect",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(80, 30),
                BackColor = System.Drawing.Color.LightGreen
            };

            // Execute button
            this.btnExecute = new Button
            {
                Name = "btnExecute",
                Text = "Execute",
                Location = new System.Drawing.Point(100, 10),
                Size = new System.Drawing.Size(80, 30),
                BackColor = System.Drawing.Color.LightBlue
            };

            // Clear button
            this.btnClear = new Button
            {
                Name = "btnClear",
                Text = "Clear",
                Location = new System.Drawing.Point(190, 10),
                Size = new System.Drawing.Size(80, 30)
            };

            // Load Script button
            this.btnLoadScript = new Button
            {
                Name = "btnLoadScript",
                Text = "Load Script",
                Location = new System.Drawing.Point(280, 10),
                Size = new System.Drawing.Size(80, 30)
            };

            // Save Script button
            this.btnSaveScript = new Button
            {
                Name = "btnSaveScript",
                Text = "Save Script",
                Location = new System.Drawing.Point(370, 10),
                Size = new System.Drawing.Size(80, 30)
            };

            // Status label
            Label lblStatus = new Label
            {
                Name = "lblStatus",
                Text = "Status: Disconnected",
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(200, 20),
                ForeColor = System.Drawing.Color.Red
            };

            controlPanel.Controls.Add(this.btnConnect);
            controlPanel.Controls.Add(this.btnExecute);
            controlPanel.Controls.Add(this.btnClear);
            controlPanel.Controls.Add(this.btnLoadScript);
            controlPanel.Controls.Add(this.btnSaveScript);
            controlPanel.Controls.Add(lblStatus);

            this.Controls.Add(controlPanel);
        }

        /// <summary>
        /// Creates the status bar at the bottom of the form.
        /// </summary>
        private void CreateStatusBar()
        {
            StatusStrip statusStrip = new StatusStrip();
            ToolStripStatusLabel lblInfo = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(lblInfo);
            this.Controls.Add(statusStrip);
        }

        // ====== EVENT HANDLERS ======

        /// <summary>
        /// Handles the form load event.
        /// </summary>
        private void RobloxExecutorForm_Load(object sender, EventArgs e)
        {
            MessageBox.Show("Roblox Executor initialized successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the form closing event.
        /// </summary>
        private void RobloxExecutorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isConnected)
            {
                DisconnectFromGame();
            }
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Handles the Connect button click event.
        /// </summary>
        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                DisconnectFromGame();
            }
            else
            {
                await ConnectToGameAsync();
            }
        }

        /// <summary>
        /// Handles the Execute button click event.
        /// </summary>
        private async void BtnExecute_Click(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Please connect to a Roblox game first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            TextBox scriptEditor = this.Controls["txtScript"] as TextBox;
            if (scriptEditor == null || string.IsNullOrWhiteSpace(scriptEditor.Text))
            {
                MessageBox.Show("Please enter a script to execute.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await ExecuteScriptAsync(scriptEditor.Text);
        }

        /// <summary>
        /// Handles the Clear button click event.
        /// </summary>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            TextBox scriptEditor = this.Controls["txtScript"] as TextBox;
            if (scriptEditor != null)
            {
                scriptEditor.Clear();
                scriptEditor.Text = "-- Roblox Lua Script Editor\n-- Start typing your script here\n";
            }
        }

        /// <summary>
        /// Handles the Load Script button click event.
        /// </summary>
        private void BtnLoadScript_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Load Roblox Script"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string scriptContent = File.ReadAllText(openFileDialog.FileName);
                    TextBox scriptEditor = this.Controls["txtScript"] as TextBox;
                    if (scriptEditor != null)
                    {
                        scriptEditor.Text = scriptContent;
                    }
                    MessageBox.Show("Script loaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading script: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Handles the Save Script button click event.
        /// </summary>
        private void BtnSaveScript_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Save Roblox Script"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    TextBox scriptEditor = this.Controls["txtScript"] as TextBox;
                    if (scriptEditor != null)
                    {
                        File.WriteAllText(saveFileDialog.FileName, scriptEditor.Text);
                    }
                    MessageBox.Show("Script saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving script: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ====== HELPER METHODS ======

        /// <summary>
        /// Connects to a Roblox game asynchronously.
        /// </summary>
        private async Task ConnectToGameAsync()
        {
            try
            {
                this.btnConnect.Enabled = false;
                this.btnConnect.Text = "Connecting...";

                // Simulate connection process
                await Task.Delay(1500);

                _isConnected = true;
                this.btnConnect.Text = "Disconnect";
                this.btnConnect.BackColor = System.Drawing.Color.LightCoral;
                
                // Update status
                Label lblStatus = this.Controls.Find("lblStatus", true).FirstOrDefault() as Label;
                if (lblStatus != null)
                {
                    lblStatus.Text = "Status: Connected";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                }

                MessageBox.Show("Connected to Roblox game successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.btnConnect.Enabled = true;
            }
        }

        /// <summary>
        /// Disconnects from the Roblox game.
        /// </summary>
        private void DisconnectFromGame()
        {
            _isConnected = false;
            this.btnConnect.Text = "Connect";
            this.btnConnect.BackColor = System.Drawing.Color.LightGreen;
            
            // Update status
            Label lblStatus = this.Controls.Find("lblStatus", true).FirstOrDefault() as Label;
            if (lblStatus != null)
            {
                lblStatus.Text = "Status: Disconnected";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }

            MessageBox.Show("Disconnected from Roblox game.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Executes a script asynchronously.
        /// </summary>
        private async Task ExecuteScriptAsync(string script)
        {
            try
            {
                this.btnExecute.Enabled = false;
                this.btnExecute.Text = "Executing...";

                // Execute script through the engine
                bool success = await _executorEngine.ExecuteScriptAsync(script);

                if (success)
                {
                    MessageBox.Show("Script executed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Script execution failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing script: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.btnExecute.Enabled = true;
                this.btnExecute.Text = "Execute";
            }
        }

        // ====== CONTROL DECLARATIONS ======

        private Button btnConnect;
        private Button btnExecute;
        private Button btnClear;
        private Button btnLoadScript;
        private Button btnSaveScript;
    }
}
