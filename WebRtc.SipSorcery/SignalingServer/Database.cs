namespace SignalingServer
{
    public static class Database
    {
        private static Dictionary<string, User> Users = new()
        {
            { "peter", new User("peter") },
            { "denis", new User("denis") },
            { "matheus", new User("matheus") },
            { "leandro", new User("leandro") },
        };

        private static Dictionary<string, Group> Groups = new()
        {
            { "A", new Group("A") },
            { "B", new Group("B") },
            { "C", new Group("C") },
            { "D", new Group("D") },
        };

        public static User GetUser(string userId)
        {
            if (Users.TryGetValue(userId, out var user))
            {
                return user;
            }

            return new User(string.Empty);
        }

        public static Group GetGroup(string groupId)
        {
            if (Groups.TryGetValue(groupId, out var group))
            {
                return group;
            }

            return new Group(string.Empty);
        }
    }
}