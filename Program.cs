using Microsoft.Data.SqlClient;

string constr = @"Data Source=.\SQLEXPRESS;Database=Northwind;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

using (var connection = new SqlConnection(constr))
{
    connection.Open();
    var db = new Northwind(connection);
    var query = db.Customers.Where(c => c.City == "London" && c.Country == "UK").Take(2);
    Console.WriteLine("Query:\n{0}\n", query);
    var list = query.ToList();
    foreach (var item in list)
    {
        Console.WriteLine("Name: {0}", item.ContactName);
    }
    Console.ReadLine();
}
