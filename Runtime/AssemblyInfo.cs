using System.Runtime.CompilerServices;

// GestureRecognizer and OwnerlessSignal, behind UIGestureDetector, are internal: they are implementation
// details, not API. They make no engine calls, so the test assembly drives them directly and the .NET test run
// in openugd/upm-tools (level 1) can run those tests without Unity.
[assembly: InternalsVisibleTo("com.openugd.corelib.widgets.tests")]
