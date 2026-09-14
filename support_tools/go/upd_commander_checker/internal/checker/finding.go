package checker

type Finding struct {
	Path     string
	Line     int
	Code     string
	Message  string
	Severity string
}
