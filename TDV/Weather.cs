/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using BPCSharedComponent.ExtendedAudio;
using System.Threading;
namespace TDV
{

    public class Weather
 {
  public enum WeatherTypes : byte
  {
   lightRain = 1,
   moderateRain = 2,
   heavyRain = 3
  }

  ////seconds of thread.sleep. will be
  ////randomly chosen
  private byte[] weatherSleepTimes = { 15, 30, 45, 60 };
  private OggBuffer rain;
  private OggBuffer thunder;
  private bool isRaining;
  ////The following are unit counts
  private byte rainTime;
  private byte rainElapsed;
  private WeatherTypes rainIntensity;

  public void start()
  {
   while (true) {
    startRain();
    Thread.Sleep(Common.getRandom(0, weatherSleepTimes.Length - 1) * 1000);
    ////weatherSleepTimes() random seconds
   }
  }

  private void startRain()
  {
   if (isRaining) {
    rainElapsed += 1;
    if (rainElapsed >= rainTime) {
     stopRain();
    }
    return;
    ////already raining
   }
   if (Common.getRandom(1, 50) == 25) {
    if (!isRaining) {
     rainIntensity = (WeatherTypes)Common.getRandom(1, 3);
     rain = DSound.loadOgg(DSound.SoundPath + "\\wr" + rainIntensity + ".ogg");
                    rain.play(true);
     isRaining = true;
    }
    ////if !isRaining
   }
   ////if random
  }
  private void stopRain()
  {
   isRaining = false;
   if ((rain != null)) {
    rain.stopOgg();
   }
   if ((thunder != null)) {
    thunder.stopOgg();
   }
   rainElapsed = 0;
   rainTime = 0;
  }
 }
}
