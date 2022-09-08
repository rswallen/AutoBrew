using BepInEx.Logging;
using System;
using System.IO;
using System.IO.Compression;

namespace AutoBrew
{
    internal static class PlotterUrlDecoder
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        public static string ConvertUrlSafeBase64ToBase64(string urlsafebase64)
        {
            // fix padding
            switch (urlsafebase64.Length % 4)
            {
                case 0:
                {
                    break;
                }
                case 3:
                {
                    urlsafebase64 += '=';
                    break;
                }
                case 2:
                {
                    urlsafebase64 += "==";
                    break;
                }
                case 1:
                {
                    Log.LogError("Base Base64 data");
                    return null;
                }
            }
            
            return urlsafebase64.Replace('_', '/').Replace('-', '+').Replace('.', '=');
        }

        public static string ProcessURL(string url)
        {
            // split: https://potionous.app/plotter?data=AXicq1ZK... into https://potionous.app/plotter and data=AXicq1ZK...
            var pieces = url.Split('?');
            if (!pieces[0].Equals("https://potionous.app/plotter"))
            {
                Log.LogError("PlotterDecoder: URL is not a potionous plotter link");
                return null;
            }

            if (pieces.Length != 2)
            {
                Log.LogError("PlotterDecoder: Potionous link has no data");
                return null;
            }

            // split data=AXicq1ZK... into data and AXicq1ZK...
            var lumps = pieces[1].Split('=');
            if (lumps.Length != 2)
            {
                Log.LogError("PlotterDecoder: Potionous link has no data");
                return null;
            }
            
            Log.LogDebug($"PlotterDecoder: Site - {pieces[0]}");
            Log.LogDebug($"PlotterDecoder: Data - {lumps[1]}");

            string base64Data = ConvertUrlSafeBase64ToBase64(lumps[1]);
            if (base64Data == null)
            {
                return null;
            }

            var rawData = Convert.FromBase64String(base64Data);
            Log.LogDebug("PlotterDecoder: Base64 conversion succeeded");

            int version = rawData[0];
            if (version != 1)
            {
                Log.LogError("$PlotterDecoder: Bad Version. Expected 1, received {version}");
                return null;
            }

            try
            {
                Log.LogDebug("PlotterDecoder: Inflating JSON");
                var unzipped = Decompress(rawData);
                if (unzipped == null)
                {
                    Log.LogError("Could not deflate");
                    return null;
                }
                return unzipped;
            }
            catch (Exception e)
            {
                Log.LogError($"Error: {e}");
            }
            return null;
        }

        public static string Decompress(byte[] data)
        {
            using var memStream = new MemoryStream(data);
            // skip version number
            memStream.ReadByte();

            // skip compression indicator bytes cos .NET is a child and can't understand them
            memStream.ReadByte();
            memStream.ReadByte();

            using var deflate = new DeflateStream(memStream, CompressionMode.Decompress);
            using var reader = new StreamReader(deflate, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
