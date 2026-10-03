using System.Runtime.CompilerServices;

// GestureRecognizer and OwnerlessSignal, behind UIGestureDetector, are internal: they are implementation
// details, not API. They make no engine calls, so the test assembly drives them directly and level 1 can run
// those tests without Unity.
[assembly: InternalsVisibleTo("com.openugd.corelib.widgets.tests")]
