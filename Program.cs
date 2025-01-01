using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rotary
{
    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint, true);
        }
    }

    public class Rotary : Form
    {
        private readonly List<NumericSpinner> spinners;
        private const int DIGIT_WIDTH = 40;
        private const int DIGIT_HEIGHT = 60;
        private const int SPACING = 5;
        private const int BUTTON_WIDTH = 100;
        private const int BUTTON_HEIGHT = 30;
        private readonly Button removeDigitButton;
        private readonly Button toggleInputButton;
        private bool inputEnabled = false;

        public Rotary()
        {
            Text = "Rotary";
            Size = new Size(400, 180);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            BackColor = SystemColors.Control;
            
            Panel mainPanel = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = SystemColors.Control
            };
            Controls.Add(mainPanel);

            var digitPanel = new DoubleBufferedFlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = SystemColors.Control
            };
            mainPanel.Controls.Add(digitPanel);

            spinners = [];

            var buttonPanel = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 3,
                Dock = DockStyle.Bottom,
                Height = BUTTON_HEIGHT + 10,
                BackColor = SystemColors.Control,
                AutoSize = false
            };

            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            var addDigitButton = CreateStandardButton("Add Digit");
            addDigitButton.Click += (s, e) => AddDigit(digitPanel);

            removeDigitButton = CreateStandardButton("Remove Digit");
            removeDigitButton.Enabled = false;
            removeDigitButton.Click += (s, e) => RemoveDigit(digitPanel);

            toggleInputButton = CreateStandardButton("Enable Input");
            toggleInputButton.Click += ToggleInput;

            buttonPanel.Controls.Add(addDigitButton, 0, 0);
            buttonPanel.Controls.Add(removeDigitButton, 1, 0);
            buttonPanel.Controls.Add(toggleInputButton, 2, 0);

            mainPanel.Controls.Add(buttonPanel);

            AddDigit(digitPanel);
        }

        private static Button CreateStandardButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = BUTTON_WIDTH,
                Height = BUTTON_HEIGHT,
                Anchor = AnchorStyles.None,
                AutoSize = false,
                UseVisualStyleBackColor = true
            };
        }

        private void ToggleInput(object sender, EventArgs e)
        {
            inputEnabled = !inputEnabled;
            toggleInputButton.Text = inputEnabled ? "Disable Input" : "Enable Input";
            
            foreach (var spinner in spinners)
            {
                spinner.InputEnabled = inputEnabled;
            }
        }

        private void UpdateRemoveButtonState()
        {
            if (removeDigitButton != null)
            {
                removeDigitButton.Enabled = spinners.Count > 1;
            }
        }

        private void AddDigit(FlowLayoutPanel panel)
        {
            NumericSpinner spinner = new()
            {
                Size = new Size(DIGIT_WIDTH, DIGIT_HEIGHT),
                Margin = new Padding(SPACING),
                InputEnabled = inputEnabled
            };
            panel.Controls.Add(spinner);
            spinners.Add(spinner);
            UpdateRemoveButtonState();
        }
        private void RemoveDigit(FlowLayoutPanel panel)
        {
            if (spinners.Count <= 1) return;

            var lastSpinner = spinners[^1];
            spinners.RemoveAt(spinners.Count - 1);
            panel.Controls.Remove(lastSpinner);
            lastSpinner.Dispose();
            UpdateRemoveButtonState();
        }
    }

    public class NumericSpinner : Control
    {
        private int value;
        private Rectangle upArrowBounds;
        private Rectangle downArrowBounds;
        private Rectangle numberBounds;
        private readonly Font numberFont;
        private readonly StringFormat stringFormat;
        private bool isEditing;
        private string editValue = "";
        private readonly Brush arrowBrush;
        private readonly Brush textBrush;
        private bool inputEnabled = false;

        public bool InputEnabled
        {
            get => inputEnabled;
            set
            {
                if (inputEnabled != value)
                {
                    inputEnabled = value;
                    isEditing = false;
                    Invalidate();
                }
            }
        }

        public NumericSpinner()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.Selectable, true);

            numberFont = new Font("Arial", 12, FontStyle.Bold);
            stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };

            arrowBrush = new SolidBrush(Color.Black);
            textBrush = new SolidBrush(Color.Black);
            BackColor = SystemColors.Control;

            UpdateBounds();
        }

        private new void UpdateBounds()
        {
            int totalHeight = Height;
            int arrowHeight = totalHeight / 3;
            int centeringOffset = (Width % 2 == 0) ? 0 : 1;

            upArrowBounds = new Rectangle(0, 0, Width, arrowHeight);
            numberBounds = new Rectangle(0, arrowHeight, Width, arrowHeight);
            downArrowBounds = new Rectangle(0, arrowHeight * 2, Width, arrowHeight + centeringOffset);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateBounds();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            DrawTriangle(g, upArrowBounds, true);
            DrawTriangle(g, downArrowBounds, false);

            using var textPath = new GraphicsPath();
            string displayText = isEditing ? editValue : value.ToString();
            textPath.AddString(
                displayText,
                numberFont.FontFamily,
                (int)numberFont.Style,
                numberFont.Size * 1.3f,
                numberBounds,
                stringFormat
            );
            g.FillPath(textBrush, textPath);
        }

        private void DrawTriangle(Graphics g, Rectangle bounds, bool pointUp)
        {
            int arrowSize = Math.Min(bounds.Width, bounds.Height) / 2;
            int centerX = bounds.Left + bounds.Width / 2;
            int centerY = bounds.Top + bounds.Height / 2;

            Point[] points = new Point[3];
            if (pointUp)
            {
                points[0] = new Point(centerX, centerY - arrowSize / 2);
                points[1] = new Point(centerX - arrowSize / 2, centerY + arrowSize / 2);
                points[2] = new Point(centerX + arrowSize / 2, centerY + arrowSize / 2);
            }
            else
            {
                points[0] = new Point(centerX, centerY + arrowSize / 2);
                points[1] = new Point(centerX - arrowSize / 2, centerY - arrowSize / 2);
                points[2] = new Point(centerX + arrowSize / 2, centerY - arrowSize / 2);
            }

            g.FillPolygon(arrowBrush, points);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (upArrowBounds.Contains(e.Location))
            {
                IncrementValue();
            }
            else if (downArrowBounds.Contains(e.Location))
            {
                DecrementValue();
            }
            else if (numberBounds.Contains(e.Location) && inputEnabled)
            {
                isEditing = true;
                editValue = "";
                Invalidate();
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (!isEditing || !inputEnabled) return;

            if (char.IsDigit(e.KeyChar))
            {
                editValue = e.KeyChar.ToString();
                value = int.Parse(editValue);
                isEditing = false;
                Invalidate();
            }
            else if (e.KeyChar == (char)Keys.Enter || e.KeyChar == (char)Keys.Escape)
            {
                isEditing = false;
                Invalidate();
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            isEditing = false;
            Invalidate();
        }

        private void IncrementValue()
        {
            value = (value + 1) % 10;
            Invalidate();
        }

        private void DecrementValue()
        {
            value = (value - 1 + 10) % 10;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                numberFont?.Dispose();
                stringFormat?.Dispose();
                arrowBrush?.Dispose();
                textBrush?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Rotary());
        }
    }
}