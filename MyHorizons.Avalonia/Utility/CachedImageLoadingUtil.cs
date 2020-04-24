using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Caching.Memory;
using MyHorizons.Data.TownData;
using System;

namespace MyHorizons.Avalonia.Utility
{
    public class ImageLoader
    {
        private const long ONE_GIGABYTE = (long)1e+9;

        private static readonly MemoryCache<Bitmap> BitmapMemoryCache = new MemoryCache<Bitmap>(
            new MemoryCacheOptions{
                SizeLimit = ONE_GIGABYTE // This is arbitrary and can represent anything.
            });

        private static readonly string[] VillagerSpeciesNameLookupTable =
        {
            "ant", "bea", "brd", "bul", "cat", "cbr", "chn", "cow", "crd", "der",
            "dog", "duk", "elp", "flg", "goa", "gor", "ham", "hip", "hrs", "kal",
            "kgr", "lon", "mnk", "mus", "ocp", "ost", "pbr", "pgn", "pig", "rbt",
            "rhn", "shp", "squ", "tig", "wol", "non"
        };

        private static Bitmap CreateBitmap(Uri uri) => new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(uri));

        private static MemoryCacheEntryOptions CreateMemoryCacheEntryOptions(IBitmap bitmap)
        {
            // Not sure how to get image size in bytes at this point but this should suffice as a size estimate
            var pixels = (long)bitmap.Size.Width * (long)bitmap.Size.Height;
            return new MemoryCacheEntryOptions { Size = pixels };
        }

        public Bitmap? LoadImageForVillager(in Villager villager)
        {
            if (villager.Species >= VillagerSpeciesNameLookupTable.Length)
                return null;
            var uri = new Uri($"resm:MyHorizons.Avalonia.Resources.{VillagerSpeciesNameLookupTable[villager.Species]}{villager.VariantIdx:d2}.png");
            return LoadCachedImage(uri);
        }

        public Bitmap LoadCachedImage(Uri uri) => BitmapMemoryCache.GetOrCreate(uri.GetHashCode(), () => CreateBitmap(uri), CreateMemoryCacheEntryOptions);
    }
}
