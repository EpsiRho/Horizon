using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace Horizon
{
    public class Contact
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public Contact(string name, string ip)
        {
            this.Name = name;
            this.IP = ip;
        }
        public Contact()
        {
            this.Name = "name";
            this.IP = "0.0.0.0";
        }
    }

    public static class ContactsAccess
    {
        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("ContactsDataBase.db", CreationCollisionOption.OpenIfExists);
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ContactsDataBase.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                String tableCommand = "CREATE TABLE IF NOT " +
                    "EXISTS MyTable (Name_Entry TEXT, " +
                    "IP_Entry TEXT)";

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        public static void AddData(string Name, string IP)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ContactsDataBase.db");
            using (SqliteConnection db =
              new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO MyTable VALUES (@Name, @IP);";
                insertCommand.Parameters.AddWithValue("@Name", Name);
                insertCommand.Parameters.AddWithValue("@IP", IP);

                insertCommand.ExecuteReader();

                db.Close();
            }

        }

        public static List<String> GetNames()
        {
            List<String> entries = new List<string>();

            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ContactsDataBase.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT Name_Entry FROM MyTable", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(query.GetString(0));
                }

                db.Close();
            }

            return entries;
        }

        public static List<String> GetIPs()
        {
            List<String> entries = new List<string>();

            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ContactsDataBase.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT IP_Entry FROM MyTable", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(query.GetString(0));
                }

                db.Close();
            }

            return entries;
        }

        public static void DeleteData(string name)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ContactsDataBase.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand deleteCommand = new SqliteCommand();

                deleteCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                deleteCommand.CommandText = "DELETE FROM MyTable WHERE Name_Entry LIKE (@Name);";
                deleteCommand.Parameters.AddWithValue("@Name", name);

                deleteCommand.ExecuteReader();
                db.Close();
            }
        }
    }

    public class ContactsViewModel
    {
        private ObservableCollection<Contact> contacts = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> Contacts { get { return this.contacts; } }
        public IOrderedEnumerable<Contact> orderedContacts;
        public ContactsViewModel()
        {
            
        }
        public void AddContact(string name, string ip)
        {
            this.contacts.Add(new Contact(name, ip));
        }
        public void RemoveContact(Contact item)
        {
            contacts.Remove(item);
        }
        public string searchByIp(string ip)
        {
            foreach(Contact item in contacts)
            {
                if(item.IP == ip)
                {
                    return item.Name;
                }
            }
            return "Unknown(" + ip + ")";
        }
        public void InsetAt(int index, Contact item)
        {
            contacts.Insert(index, item);
        }
    }
}
