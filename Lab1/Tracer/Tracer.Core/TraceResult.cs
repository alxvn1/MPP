using System.Collections.Generic;

namespace Tracer.Core;

public record TraceResult(IReadOnlyList<ThreadResult> Threads);