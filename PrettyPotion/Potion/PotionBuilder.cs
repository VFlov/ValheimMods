using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyPotion
{
    internal class PotionBuilder : IPotionBuilder
    {
        //Присваивать false нету смысла так как при обьявлении он будет присвоен по умолчанию
        private bool _hasMushroom;
        private bool _hasYellowMushroom;
        private bool _hasYotunMushroom;
        private bool _hasMistyMushrooms;
        private bool _hasBunch;
        private bool _hasThistle;
        private bool _hasHulk;
        private bool _hasBerries;

        public IPotionBuilder AddMushroom()
        {
            _hasMushroom = true;
            return this; // Возвращаем интерфейс
        }
        public IPotionBuilder AddYellowMushroom()
        {
            _hasYellowMushroom = true;
            return this;
        }

        public IPotionBuilder AddYotunMushroom()
        {
            _hasYotunMushroom = true;
            return this;
        }

        public IPotionBuilder AddMistyMushroom()
        {
            _hasMistyMushrooms = true;
            return this;
        }

        public IPotionBuilder AddBunch()
        {
            _hasBunch = true;
            return this;
        }

        public IPotionBuilder AddThistle()
        {
            _hasThistle = true;
            return this;
        }

        public IPotionBuilder AddHulk()
        {
            _hasHulk = true;
            return this;
        }
        public IPotionBuilder AddBerries()
        {
            _hasBerries = true;
            return this;
        }

        public Potion Build()
        {
            return new Potion(_hasMushroom, _hasYellowMushroom, _hasYotunMushroom, _hasMistyMushrooms,
                              _hasBunch, _hasThistle, _hasHulk,
                              _hasBerries);
        }
    }
}
