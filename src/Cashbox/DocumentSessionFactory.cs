namespace Cashbox
{
    using Implementations;


    public class DocumentSessionFactory
	{
		public static IDocumentSession Create(string filename)
		{
			return new DocumentSession(filename);
		}
	}
}