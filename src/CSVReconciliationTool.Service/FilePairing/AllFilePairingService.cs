using CSVReconciliationTool.Core;
using CSVReconciliationTool.Service.FilePairing.Interface;
namespace CSVReconciliationTool.Service.FilePairing;

public class AllFilePairingService : IFilePairingService
{
    public AllFilePairingService()
    {
        
    }
    public Task FileMatchAsync(string source, string destination, FilePairMode filePairMode)
    {
        throw new NotImplementedException();
    }
}