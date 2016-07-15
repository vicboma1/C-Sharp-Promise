using System;

public interface IPromiseInfo {
	int Id { get; set; }
	string Name { get; set; }
	PromiseStateEnum State { get; set; }
}