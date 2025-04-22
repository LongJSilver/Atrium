using System.Collections.Generic;

namespace Atrium
{
    public interface IHistoryStorageService
    {
        IEnumerable<string> ReadList();
        void StoreList(List<string> history);
    }
}