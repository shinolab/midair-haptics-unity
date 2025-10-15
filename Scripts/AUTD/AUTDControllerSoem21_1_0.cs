#if AUTD21_1_0


using AUTD3Sharp;
using AUTD3Sharp.Gain.Holo;
using static AUTD3Sharp.Gain.Holo.Amplitude.Units;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
#nullable enable
#endif


public class AUTDControllerSoem21_1_0 : AUTDControllerGen21_1_0<AUTD3Sharp.Link.SOEM>
{

     public override void OpenAUTD(ControllerBuilder builder)
    {
     _autd = builder.OpenWith(AUTD3Sharp.Link.SOEM.Builder());
    }

 }

#if UNITY_2020_2_OR_NEWER
#nullable disable
#endif

#endif
