namespace PowerSupplyApp.TUI
{
    public class NumericDataString
    {
        private bool modified = false;
        public bool Modified
        {
            get { return modified; }
            set
            {
                modified = value;
            }
        }

        private bool selected = false;
        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
            }
        }

        private int selectedDigitIndex = -1;
        public int SelectedDigitIndex
        {
            get { return selectedDigitIndex; }
            set
            {
                selectedDigitIndex = value;
            }
        }

        private string rawLeftTextValue = string.Empty;
        private string rawRightTextValue = string.Empty;
        private string paddedTextValue = null;
        private string markupText = string.Empty;

        private int RawValue { get; set; } = -1;
        private int NumChars { get; set; }
        private int PointIndex { get; set; }

        public NumericDataString(int numChars = 5, int pointIndex = -1)
        {
            NumChars = numChars;
            PointIndex = pointIndex;
        }

        public void UpdateValue(ushort value)
        {
            RawValue = value;
            rawLeftTextValue = $"{RawValue}";
            rawRightTextValue = string.Empty;
            paddedTextValue = string.Empty;

            if (PointIndex >= 0)
            {
                // Ensure there is at least one digit to the left of the decimal point.
                if (PointIndex + 1 > rawLeftTextValue.Length)
                {
                    rawLeftTextValue = rawLeftTextValue.PadLeft(PointIndex + 1, '0');
                }

                rawRightTextValue = rawLeftTextValue.Substring(rawLeftTextValue.Length - PointIndex);
                rawLeftTextValue = rawLeftTextValue.Substring(0, rawLeftTextValue.Length - PointIndex);
                paddedTextValue = "".PadLeft(NumChars - rawLeftTextValue.Length - rawRightTextValue.Length - 1, '0');
            }
            else
            {
                paddedTextValue = "".PadLeft(NumChars - rawLeftTextValue.Length, '0');
            }
        }

        public string ToMarkupString()
        {
            if (Selected && SelectedDigitIndex >= 0)
            {
                if (SelectedDigitIndex < rawRightTextValue.Length)
                {
                    int rightIndex = rawRightTextValue.Length - SelectedDigitIndex - 1;
                    char digit = rawRightTextValue[rightIndex];
                    rawRightTextValue = rawRightTextValue.Remove(rightIndex, 1).Insert(rightIndex, $"[underline]{digit}[/]");
                }
                else if (SelectedDigitIndex < rawLeftTextValue.Length + rawRightTextValue.Length)
                {
                    int leftIndex = rawLeftTextValue.Length - (SelectedDigitIndex - rawRightTextValue.Length) - 1;
                    if (leftIndex < rawLeftTextValue.Length && leftIndex >= 0)
                    {
                        char digit = rawLeftTextValue[leftIndex];
                        rawLeftTextValue = rawLeftTextValue.Remove(leftIndex, 1).Insert(leftIndex, $"[underline]{digit}[/]");
                    }
                }
                else
                {
                    int padIndex = paddedTextValue.Length - (SelectedDigitIndex - rawLeftTextValue.Length - rawRightTextValue.Length) - 1;
                    if (padIndex < paddedTextValue.Length && padIndex >= 0)
                    {
                        char digit = paddedTextValue[padIndex];
                        paddedTextValue = paddedTextValue.Remove(padIndex, 1).Insert(padIndex, $"[underline]{digit}[/]");
                    }
                }
            }

            if (paddedTextValue.Length > 0)
            {
                markupText = $"[dim]{paddedTextValue}[/]";
            }
            else
            {
                markupText = string.Empty;
            }

            if (rawRightTextValue.Length > 0)
            {
                markupText += $"[white]{rawLeftTextValue}.{rawRightTextValue}[/]";
            }
            else
            {
                markupText += $"[white]{rawLeftTextValue}[/]";
            }

            if (Modified)
            {
                markupText = $"[blink][invert]{markupText}[/][/]";
            }

            return markupText;
        }
    }
}
