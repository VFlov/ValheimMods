using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyPotion
{
    internal class Potion
    {
        private bool _hasMushroom;
        private bool _hasYellowMushroom;
        private bool _hasYotunMushroom;
        private bool _hasMistyMushrooms;
        private bool _hasBunch;
        private bool _hasThistle;
        private bool _hasHulk;
        private bool _hasBerries;

        internal Potion(bool hasMushroom, bool hasYellowMushroom, bool hasYotunMushroom, bool hasMistyMushrooms,
                      bool hasBunch, bool hasThistle, bool hasHulk,
                      bool hasBerries)
        {
            _hasMushroom = hasMushroom;
            _hasYellowMushroom = hasYellowMushroom;
            _hasYotunMushroom = hasYotunMushroom;
            _hasMistyMushrooms = hasMistyMushrooms;
            _hasBunch = hasBunch;
            _hasThistle = hasThistle;
            _hasHulk = hasHulk;
            _hasBerries = hasBerries;
        }

    }
}
