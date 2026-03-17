using System;
using System.Collections.Generic;
using System.Text;

namespace RotationSolver.ExtraRotations;

public enum OpenerState
{
    OpenerNotReady,
    OpenerReady,
    InOpener,
    OpenerFinished,
    FailedOpener
}
