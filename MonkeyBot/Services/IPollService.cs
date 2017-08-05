using MonkeyBot.Services.Common.Poll;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPollService
    {
        Task AddPollAsync(Poll poll);
    }
}
