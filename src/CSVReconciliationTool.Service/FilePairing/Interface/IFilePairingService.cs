using CSVReconciliationTool.Core;

namespace CSVReconciliationTool.Service.FilePairing.Interface;

public interface IFilePairingService
{
    Task FileMatchAsync(string source, string destination, FilePairMode filePairMode);
}