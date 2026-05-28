using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class ModelDbExtensions
{
    //Will require language version set to 14 to use.
    extension(ModelDb)
    {
        public static T CardModifier<T>() where T : CardModifier
        {
            return ModelDb.Get<T>();
        }
    }

    public static T GetCardModifier<T>() where T : CardModifier
    {
        return ModelDb.CardModifier<T>();
    }
}