using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace TwoBySixAntennaSwitch.Lcd
{
    public class DfRobotLcdShield : Lcd
    {

        readonly AnalogInput analog;

        public delegate void ButtonPressHandler(object sender, Buttons buttonPressed);
        public event ButtonPressHandler OnButtonPressed;

        const int KEY_UP = 600;
        const int KEY_DOWN = 1500;
        const int KEY_CMD1 = 2100;
        const int KEY_CMD2 = 100;
        const int KEY_CMD3 = 3000;
        const int KEY_NONE = 4000;

        private const int DEBOUNCE = 50;

        public DfRobotLcdShield(byte columns = 16, byte rows = 2)
            : base(new GpioLcdTransferProvider(

                Pins.GPIO_PIN_D8,
                Pins.GPIO_PIN_D9,
                Pins.GPIO_PIN_D4,
                Pins.GPIO_PIN_D5,
                Pins.GPIO_PIN_D6,
                Pins.GPIO_PIN_D7))
        {

            analog = new AnalogInput(AnalogChannels.ANALOG_PIN_A0);

            this.Begin(columns, rows);
            this.Clear();

            var buttonReader = new Thread(ReadButtons);
            buttonReader.Start();

        }

        public enum Buttons
        {
            Up,
            Down,
            Command1,
            Command2,
            Command3,
            None
        }

        private void ReadButtons()
        {
            var previousButton = Buttons.None;

            while (true)
            {
                var currentButton = ButtonState;

                if (currentButton != previousButton)
                {
                    if (OnButtonPressed != null)
                    {
                        OnButtonPressed(this, ButtonState);
                    }
                    previousButton = currentButton;
                }
                Thread.Sleep(DEBOUNCE);
            }
        }

        public Buttons ButtonState
        {
            get
            {
                double adcRaw = analog.ReadRaw();
                //Debug.Print(adcRaw.ToString());
                if (adcRaw > KEY_NONE) return Buttons.None;
                if (adcRaw < KEY_CMD2) return Buttons.Command2;
                if (adcRaw < KEY_UP) return Buttons.Up;
                if (adcRaw < KEY_DOWN) return Buttons.Down;
                if (adcRaw < KEY_CMD1) return Buttons.Command1;
                if (adcRaw < KEY_CMD3) return Buttons.Command3;
                return Buttons.None;
            }
        }

        /// <summary>
        /// Writes an entire line of text to the LCD.
        /// </summary>
        /// <param name="line">The line required to write at.  Starts at 1.</param>
        /// <param name="text">The string to write.</param>
        /// <param name="align">Alignment of the string. EG: Left</param>
        public void WriteLine(int line, string text, TextAlign align = TextAlign.Left)
        {
            if (line < 0) { line = 0; }
            if (line > Lines - 1) { line = Lines - 1; }
            SetCursorPosition(0, line);
            WriteLine(text, align);
        }

        /// <summary>
        /// Writes an entire line of text to the LCD.
        /// </summary>
        /// <param name="text">The string to write.</param>
        /// <param name="align">Alignment of the string. EG: Left</param>
        public void WriteLine(string text, TextAlign align = TextAlign.Left)
        {

            if (text.Length > Columns)
            {
                // the string length was too long to fit on a line so trim it.
                text = text.Substring(0, Columns);
            }
            else if (text.Length < Columns)
            {
                // the string length was too short to fit on a line so pad it.
                var numberOfCharsToPad = Columns - text.Length;

                for (int i = 0; i < numberOfCharsToPad; i++)
                {
                    switch (align)
                    {
                        case TextAlign.Left:
                            text = text + " ";
                            break;
                        case TextAlign.Right:
                            text = " " + text;
                            break;
                        case TextAlign.Centre:
                            if (text.Length % 2 == 0)
                            { text = " " + text; }
                            else { text = text + " "; }
                            break;
                    }
                }
            }

            // write the string
            Write(text);

        }

    }

}