using System.Collections.Generic;

namespace Tracer.Core;

public record ThreadResult(int Id, long Time, IReadOnlyList<MethodResult> Methods);