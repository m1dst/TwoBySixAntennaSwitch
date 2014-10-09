using System;
using Microsoft.SPOT;

namespace TwoBySixAntennaSwitch
{
    public class Radio
    {

        public Radio()
        {
            CurrentBand = RadioBand.B20;
            BandDecodingMethod = BandDecodingMethod.YaesuBcd;
            RadioState = RadioState.Rx;
            BandPassFilterType = BandPassFilterType.None;
        }

        public RadioBand CurrentBand { get; set; }

        public BandDecodingMethod BandDecodingMethod { get; set; }

        public RadioState RadioState { get; set; }

        public int CurrentAntenna { get; set; }

        public BandPassFilterType BandPassFilterType { get; set; }

    }
}
