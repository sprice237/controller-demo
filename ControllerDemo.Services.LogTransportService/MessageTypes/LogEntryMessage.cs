using ControllerDemo.Rabbit;
using ControllerDemo.Services.LogTransportService.Models;

namespace ControllerDemo.Services.LogTransportService.MessageTypes
{
    public class LogEntryMessage : BaseMessage
    {
        public override string QueueName => "log-entries";
        public JournalLogEntry JournalLogEntry { get; set; }
    }
}