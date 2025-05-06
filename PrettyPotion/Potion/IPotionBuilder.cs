using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyPotion
{
    internal interface IPotionBuilder
    {
        IPotionBuilder AddMushroom(); //Для чего нам возвращать интерфейс расскажу ниже
        IPotionBuilder AddYellowMushroom();
        IPotionBuilder AddYotunMushroom();
        IPotionBuilder AddMistyMushroom();
        IPotionBuilder AddBunch();
        IPotionBuilder AddThistle();
        IPotionBuilder AddHulk();
        IPotionBuilder AddBerries();
        Potion Build();
    }
}
