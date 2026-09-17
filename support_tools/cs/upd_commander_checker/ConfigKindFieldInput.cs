using System.Text.Json;

namespace UpdCommanderChecker;

internal sealed record ConfigKindFieldInput(JsonElement Root, string Name, JsonValueKind Expected);
