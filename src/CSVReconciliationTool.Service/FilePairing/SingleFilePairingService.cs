using CSVReconciliationTool.Core;
using CSVReconciliationTool.Service.FilePairing.Interface;

namespace CSVReconciliationTool.Service.FilePairing;

public class SingleFilePairingService : IFilePairingService
{
    public SingleFilePairingService()
    {
        
    }

    public Task FileMatchAsync(string source, string destination, FilePairMode filePairMode)
    {
        throw new NotImplementedException();
    }
}