using System.Collections.Generic;

namespace MitsukuApi
{
	public class MitsukuResponse
	{
		public string Status { get; set; }
		public List<string> Responses { get; set; }
		public string SessionId { get; set; }
		public string Channel { get; set; }
	}
}
