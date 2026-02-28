using System;

namespace Raffe.Models;

public class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
