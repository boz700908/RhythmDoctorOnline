using System;
using UnityEngine;

namespace Credits
{
    /// <summary>
    /// Weather state for the credits widget. Port of animation_classes.Weather.
    /// </summary>
    public class Weather
    {
        public float Precip { get; set; }
        public float Temp { get; set; }
        public float Wind { get; set; }
        public float Gust { get; set; }
        public int WindDir { get; set; }
        public float Humidity { get; set; }
        public string WeatherName { get; set; }
        public int Days { get; set; }

        public Weather(float precip, float temp, float wind, float gust, int windDir, float humidity, int days = 2)
        {
            Precip = precip;
            Temp = temp;
            Wind = wind;
            Gust = gust;
            WindDir = windDir;
            Humidity = humidity;
            Days = days;
            WeatherName = GetWeatherName();
        }

        public string GetWeatherName()
        {
            if (Humidity > 0.5f)
            {
                if (Precip > 0.4f)
                {
                    if (Wind > 43) return Temp < 32 ? "Blizzard" : "Hurricane";
                    if (Wind > 25) return Temp < 32 ? "Snowstorm" : "Storm";
                    return Temp < 32 ? "Snow" : "Rain";
                }
                if (Precip > 0.25f) return Temp < 32 ? "Sleet" : "Drizzle";
                if (Humidity > 0.8f || Precip > 0.5f) return "Overcast";
                if (Humidity > 0.65f || Precip > 0.3f) return "Cloudy";
                return "Partly cloudy";
            }
            if (Precip > 0.4f) return Temp < 32 ? "Snow" : "Rain";
            if (Precip > 0.2f) return Temp < 32 ? "Sleet" : "Drizzle";
            return (Humidity < 0.1f ? "Sunny" : Humidity < 0.2f ? "Partly sunny" : "Clear");
        }

        public void Mutate(int steps = 1)
        {
            for (int s = 0; s < steps; s++)
            {
                float precipMove = UnityEngine.Random.Range(-100, 101) / 400f;
                precipMove += (UnityEngine.Random.Range(0, (int)(100 * Mathf.Abs(0.33f - Precip)) + 1) / 200f) * (0.33f - Precip < 0 ? -1 : 1);
                Precip = Mathf.Clamp01(Precip + precipMove);

                WindDir = (WindDir + UnityEngine.Random.Range(-3, 4)) % 8;
                if (WindDir < 0) WindDir += 8;

                float windMove = UnityEngine.Random.Range(-100, 101) / 13f;
                windMove += (UnityEngine.Random.Range(0, (int)Mathf.Abs(15 - Wind) + 1) / 3f) * (15 - Wind < 0 ? -1 : 1);
                Wind = Mathf.Max(0, Wind + windMove);

                float adjustedDays = Days - 282;
                float tempTarget = 40f * (Mathf.Sin((2f * Mathf.PI * adjustedDays) / 365f - Mathf.PI / 3f) + 1f) + 20f;
                float tempMove = UnityEngine.Random.Range(-100, 101) / 20f;
                tempMove += (UnityEngine.Random.Range(0, (int)Mathf.Abs(tempTarget - Temp) + 1) / 5f) * (tempTarget - Temp < 0 ? -1 : 1);
                Temp = Mathf.Max(0, Temp + tempMove);

                float humidityMove = UnityEngine.Random.Range(-100, 101) / 500f;
                humidityMove += (UnityEngine.Random.Range(0, (int)Mathf.Abs(0.2f - Humidity) * 200 + 1) / 200f) * (0.5f - Humidity < 0 ? -1 : 1);
                Humidity = Mathf.Clamp01(Humidity + humidityMove);

                Gust = Wind * UnityEngine.Random.Range(210, 261) * 0.01f;
                Days++;
            }
            WeatherName = GetWeatherName();
        }
    }

    /// <summary>
    /// Known weather presets. known_weathers[4] is "Connection lost...".
    /// </summary>
    public static class KnownWeathers
    {
        private static Weather[] _weathers;

        public static Weather[] Get()
        {
            if (_weathers != null) return _weathers;
            _weathers = new[]
            {
                new Weather(0.203f, 43, 13, 25, 6, 0.66f),
                new Weather(0.04f, 52, 12, 25, 7, 0.1f),
                new Weather(0.07f, 48, 8, 20, 1, 0.1f, 200),
                new Weather(0.07f, 48, 8, 20, 1, 0.1f, 528 + 200),
                new Weather(-1, -1, -1, -1, -1, -1)
            };
            _weathers[4].WeatherName = "Connection lost...      ";
            return _weathers;
        }
    }
}
