package checker

import (
	"bytes"
	"encoding/json"
)

func decodeConfigString(value json.RawMessage, target *string) bool {
	if isJSONNull(value) {
		return false
	}
	return json.Unmarshal(value, target) == nil
}

func decodeConfigBool(value json.RawMessage, target *bool) bool {
	if isJSONNull(value) {
		return false
	}
	return json.Unmarshal(value, target) == nil
}

func decodeConfigPercent(value json.RawMessage, target *int) bool {
	if isJSONNull(value) {
		return false
	}
	var decoded int
	if json.Unmarshal(value, &decoded) != nil || decoded < 1 || decoded > 100 {
		return false
	}
	*target = decoded
	return true
}

func decodeConfigMinInt(value json.RawMessage, target *int, minimum int) bool {
	if isJSONNull(value) {
		return false
	}
	var decoded int
	if json.Unmarshal(value, &decoded) != nil || decoded < minimum {
		return false
	}
	*target = decoded
	return true
}

func decodeConfigPositiveInt(value json.RawMessage, target *int) bool {
	if isJSONNull(value) {
		return false
	}
	var decoded int
	if json.Unmarshal(value, &decoded) != nil || decoded < 1 {
		return false
	}
	*target = decoded
	return true
}

func decodeConfigStrings(value json.RawMessage, target *[]string) bool {
	if isJSONNull(value) {
		return false
	}

	var rawItems []json.RawMessage
	if json.Unmarshal(value, &rawItems) != nil {
		return false
	}

	items := make([]string, 0, len(rawItems))
	for _, rawItem := range rawItems {
		var item string
		if !decodeConfigString(rawItem, &item) {
			return false
		}
		items = append(items, item)
	}
	*target = items
	return true
}

func isJSONNull(value json.RawMessage) bool {
	return bytes.Equal(bytes.TrimSpace(value), []byte("null"))
}
