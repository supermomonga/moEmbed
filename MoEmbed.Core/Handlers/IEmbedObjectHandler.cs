using System;
using MoEmbed.Models;

namespace MoEmbed.Handlers
{
    public interface IEmbedObjectHandler
    {
        bool CanHandle(Uri uri);

        EmbedObject GetEmbedObject(Uri uri);
    }
}
