using System.Text.Json;

namespace UpdCommanderChecker;

internal sealed record ConfigFieldInput(JsonElement Root, string Name);
