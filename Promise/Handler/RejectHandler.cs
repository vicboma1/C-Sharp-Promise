using System;

public struct RejectHandler {
	public Action<Exception> callback;
	public IRejectable rejectable;
}