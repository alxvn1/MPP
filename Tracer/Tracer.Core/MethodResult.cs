using System.Collections.Generic;

namespace Tracer.Core;

public record MethodResult(string Name, string ClassName, long Time, IReadOnlyList<MethodResult> Methods);