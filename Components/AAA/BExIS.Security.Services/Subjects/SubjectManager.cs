﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Entities.Subjects;
using Vaiona.Persistence.Api;

                    
namespace BExIS.Security.Services.Subjects
{
    public sealed class SubjectManager : ISubjectManager
    {
        public SubjectManager()
        {
            IUnitOfWork uow = this.GetUnitOfWork();

            this.RolesRepo = uow.GetReadOnlyRepository<Role>();
            this.SecurityUsersRepo = uow.GetReadOnlyRepository<SecurityUser>();
            this.SubjectsRepo = uow.GetReadOnlyRepository<Subject>();
            this.UsersRepo = uow.GetReadOnlyRepository<User>();
        }

        #region Data Readers

        public IReadOnlyRepository<Role> RolesRepo { get; private set; }
        public IReadOnlyRepository<SecurityUser> SecurityUsersRepo { get; private set; }
        public IReadOnlyRepository<Subject> SubjectsRepo { get; private set; }   
        public IReadOnlyRepository<User> UsersRepo { get; private set; }      

        #endregion

        #region Attributes

        public bool AutoApproval
        {
            get { return true; }
        }

        public string MachineKey
        {
            get { return "qwertzuioplkjhgfdsayxcvbqwertztu"; }
        }

        public int MaxPasswordFailureAttempts
        {
            get { return 10; }
        }

        public int MaxSecurityAnswerFailureAttempts
        {
            get { return 10; }
        }

        public int OnlineWindow
        {
            get { return 15; }
        }

        public int PasswordFailureAttemptsWindow
        {
            get { return 30; }
        }

        public string PasswordStrengthRegularExpression
        {
            //get { return @"((?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[\W]).{6,20})"; }
            get { return @"^(.{6,24})$"; }
        }

        public int SecurityAnswerFailureAttemptsWindow
        {
            get { return 30; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public int AddUserToRole(string userName, string roleName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));

            Role role = GetRoleByName(roleName);
            
            if (role != null)
            {
                User user = GetUserByName(userName);

                if (user != null)
                {
                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<Role> repo = uow.GetRepository<Role>();

                        repo.LoadIfNot(role.Users);
                        if (!role.Users.Contains(user))
                        {
                            role.Users.Add(user);
                            user.Roles.Add(role);
                            uow.Commit();
                        }
                    }

                    return 0;
                }
                else
                {
                    return 12;
                }
            }
            else
            {
                return 11;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public int AddUserToRole(long userId, long roleId)
        {
            Contract.Requires(userId > 0);
            Contract.Requires(roleId > 0);

            Role role = GetRoleById(roleId);

            if (role != null)
            {
                User user = GetUserById(userId);

                if (user != null)
                {
                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<Role> rolesRepo = uow.GetRepository<Role>();

                        rolesRepo.LoadIfNot(role.Users);
                        if (!role.Users.Contains(user))
                        {
                            role.Users.Add(user);
                            user.Roles.Add(role);
                            uow.Commit();
                        }
                    }

                    return 0;
                }
                else
                {
                    return 12;
                }
            }
            else
            {
                return 11;
            }
        }

        public bool ApproveUser(string userName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public bool ChangePassword(string userName, string password, string newPassword)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(password));
            Contract.Requires(!String.IsNullOrWhiteSpace(newPassword));

            User user = GetUserByName(userName);
            SecurityUser securityUser = GetSecurityUserByName(userName);

            if (securityUser != null && user != null)
            {
                if (ValidateSecurityProperty(password, securityUser.Password, DecodeSalt(securityUser.PasswordSalt)))
                {
                    string passwordSalt = GenerateSalt();

                    user.LastActivityDate = DateTime.Now;
                    user.LastPasswordChangeDate = DateTime.Now;
                    securityUser.Password = EncodeSecurityProperty(newPassword, passwordSalt);
                    securityUser.PasswordSalt = EncodeSalt(passwordSalt);

                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<User> userRepo = uow.GetRepository<User>();
                        IRepository<SecurityUser> securityUserRepo = uow.GetRepository<SecurityUser>();

                        userRepo.Put(user);
                        securityUserRepo.Put(securityUser);

                        uow.Commit();
                    }

                    return (true);
                }
                else
                {
                    return (false);
                }
            }
            else
            {
                return (false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="newPasswordQuestion"></param>
        /// <param name="newPasswordAnswer"></param>
        /// <returns></returns>
        public bool ChangeSecurityQuestionAndSecurityAnswer(string userName, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(password));
            Contract.Requires(!String.IsNullOrWhiteSpace(newPasswordQuestion));
            Contract.Requires(!String.IsNullOrWhiteSpace(newPasswordAnswer));

            User user = GetUserByName(userName);
            SecurityUser securityUser = GetSecurityUserByName(userName);

            if (securityUser != null && user != null)
            {
                if (ValidateSecurityProperty(password, securityUser.Password, DecodeSalt(securityUser.SecurityAnswerSalt)))
                {
                    string securityAnswerSalt = GenerateSalt();

                    user.LastActivityDate = DateTime.Now;
                    user.LastPasswordChangeDate = DateTime.Now;
                    securityUser.SecurityAnswer = EncodeSecurityProperty(newPasswordAnswer, securityAnswerSalt);
                    securityUser.SecurityAnswerSalt = EncodeSalt(securityAnswerSalt);
                    securityUser.SecurityQuestion = newPasswordQuestion;

                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<User> repo = uow.GetRepository<User>();
                        repo.Put(user);
                        uow.Commit();
                    }

                    return (true);
                }
                else
                {
                    return (false);
                }
            }
            else
            {
                return (false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="description"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public Role CreateRole(string roleName, string description, out RoleCreateStatus status)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));
            Contract.Requires(!String.IsNullOrWhiteSpace(description));

            if (ExistsRoleName(roleName))
            {
                status = RoleCreateStatus.DuplicateRoleName;
                return null;
            }

            Role role = new Role()
            {
                // Subject Properties
                Name = roleName,

                // Role Properties
                Description = description
            };

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Role> rolesRepo = uow.GetRepository<Role>();
                rolesRepo.Put(role);
                uow.Commit();
            }

            status = RoleCreateStatus.Success;
            return (role);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="passwordQuestion"></param>
        /// <param name="passwordAnswer"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public User CreateUser(string userName, string email, string password, string passwordQuestion, string passwordAnswer, out UserCreateStatus status)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(email));
            Contract.Requires(!String.IsNullOrWhiteSpace(password));
            Contract.Requires(!String.IsNullOrWhiteSpace(passwordQuestion));
            Contract.Requires(!String.IsNullOrWhiteSpace(passwordAnswer));

            if (!String.IsNullOrWhiteSpace(GetUserNameByEmail(email)))
            {
                status = UserCreateStatus.DuplicateEmail;
                return null;
            }

            if (!Regex.Match(password, PasswordStrengthRegularExpression).Success)
            {
                status = UserCreateStatus.InvalidPassword;
                return null;
            }

            if (GetUserByName(userName) != null)
            {
                status = UserCreateStatus.DuplicateUserName;
                return null;
            }

            string passwordSalt = GenerateSalt();
            string securityAnswerSalt = GenerateSalt();

            User user = new User()
            {
                // Subject Properties
                Name = userName,

                // User Properties
                Email = email,

                LastActivityDate = DateTime.Now,
                LastLockOutDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
                LastPasswordChangeDate = DateTime.Now,
                RegistrationDate = DateTime.Now,
                IsApproved = AutoApproval,
                IsLockedOut = false
            };

            SecurityUser securityUser = new SecurityUser()
            {
                Name = userName,

                Password = EncodeSecurityProperty(password, passwordSalt),
                PasswordSalt = EncodeSalt(passwordSalt),
                SecurityQuestion = passwordQuestion,
                SecurityAnswer = EncodeSecurityProperty(passwordAnswer, securityAnswerSalt),
                SecurityAnswerSalt = EncodeSalt(securityAnswerSalt),
                PasswordFailureCount = 0,
                SecurityAnswerFailureCount = 0,
                LastSecurityAnswerFailureDate = DateTime.Now,
                LastPasswordFailureDate = DateTime.Now
            };

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<User> userRepo = uow.GetRepository<User>();
                IRepository<SecurityUser> securityUserRepo = uow.GetRepository<SecurityUser>();

                userRepo.Put(user);
                securityUserRepo.Put(securityUser);

                uow.Commit();
            }

            status = UserCreateStatus.Success;
            return (user);
        }

        private string DecodeSalt(string salt)
        {
            // Variables
            TripleDESCryptoServiceProvider encryptionEncoder = new TripleDESCryptoServiceProvider();

            encryptionEncoder.Mode = CipherMode.ECB;
            encryptionEncoder.Key = Convert.FromBase64String(MachineKey);
            encryptionEncoder.Padding = PaddingMode.None;

            ICryptoTransform DESDecrypt = encryptionEncoder.CreateDecryptor();

            // Computations
            return Encoding.Unicode.GetString(DESDecrypt.TransformFinalBlock(Convert.FromBase64String(salt), 0, Convert.FromBase64String(salt).Length));
        }

        public bool DeleteRoleById(long id)
        {
            Role role = GetRoleById(id);

            if (role != null)
            {
                using (IUnitOfWork uow = this.GetUnitOfWork())
                {
                    IRepository<Role> rolesRepo = uow.GetRepository<Role>();

                    rolesRepo.LoadIfNot(role.Users);
                    role.Users.Clear();

                    rolesRepo.Delete(role);
                    uow.Commit();
                }

                return (true);
            }

            return (false);
        }

        public bool DeleteRoleByName(string roleName)
        {
            Role role = GetRoleByName(roleName);

            if (role != null)
            {
                using (IUnitOfWork uow = this.GetUnitOfWork())
                {
                    IRepository<Role> rolesRepo = uow.GetRepository<Role>();

                    rolesRepo.LoadIfNot(role.Users);
                    role.Users.Clear();

                    rolesRepo.Delete(role);
                    uow.Commit();
                }

                return (true);
            }

            return (false);
        }

        public bool DeleteUserById(long id)
        {
            User user = GetUserById(id);

            if (user != null && !String.IsNullOrWhiteSpace(user.Name))
            {
                SecurityUser securityUser = GetSecurityUserByName(user.Name);

                if (securityUser != null)
                {
                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<Role> rolesRepo = uow.GetRepository<Role>();
                        IRepository<User> usersRepo = uow.GetRepository<User>();
                        IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                        foreach (Role role in user.Roles)
                        {
                            rolesRepo.LoadIfNot(role.Users);
                            role.Users.Remove(user);
                        }

                        usersRepo.Delete(user);
                        securityUsersRepo.Delete(securityUser);

                        uow.Commit();
                    }

                    return (true);
                }
            }

            return (false);
        }

        public bool DeleteUserByName(string userName)
        {
            User user = GetUserByName(userName);

            if (user != null)
            {
                SecurityUser securityUser = GetSecurityUserByName(userName);

                if (securityUser != null)
                {
                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<Role> rolesRepo = uow.GetRepository<Role>();
                        IRepository<User> usersRepo = uow.GetRepository<User>();
                        IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                        foreach (Role role in user.Roles)
                        {
                            rolesRepo.LoadIfNot(role.Users);
                            role.Users.Remove(user);
                        }

                        usersRepo.Delete(user);
                        securityUsersRepo.Delete(securityUser);

                        uow.Commit();
                    }

                    return (true);
                }
            }

            return (false);
        }

        private string EncodeSalt(string salt)
        {
            // Variables
            TripleDESCryptoServiceProvider encryptionEncoder = new TripleDESCryptoServiceProvider();

            encryptionEncoder.Mode = CipherMode.ECB;
            byte[] a = Convert.FromBase64String(MachineKey);
            encryptionEncoder.Key = Convert.FromBase64String(MachineKey);
            encryptionEncoder.Padding = PaddingMode.None;

            ICryptoTransform DESEncrypt = encryptionEncoder.CreateEncryptor();

            // Computations
            return Convert.ToBase64String(DESEncrypt.TransformFinalBlock(Encoding.Unicode.GetBytes(salt), 0, Encoding.Unicode.GetBytes(salt).Length));
        }

        private string EncodeSecurityProperty(string value, string salt)
        {
            // Variables
            HMACSHA256 encryptionEncoder = new HMACSHA256(Convert.FromBase64String(salt));

            byte[] valueArray = Encoding.Unicode.GetBytes(value);

            // Computations
            return Convert.ToBase64String(encryptionEncoder.ComputeHash(valueArray, 0, valueArray.Length));
        }

        public bool ExistsRoleId(long id)
        {
            if (RolesRepo.Query(r => r.Id == id).Count() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ExistsRoleName(string roleName)
        {
            if (RolesRepo.Query(r => r.Name == roleName).Count() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ExistsUserId(long id)
        {
            if (UsersRepo.Query(u => u.Id == id).Count() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ExistsUserName(string userName)
        {
            if (UsersRepo.Query(u => u.Name == userName).Count() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IQueryable<User> FindUsersByEmail(string emailToMatch)
        {
            return UsersRepo.Query(u => u.Email.ToLower().Contains(emailToMatch.ToLower()));
        }

        public IQueryable<User> FindUserByName(string userNameToMatch)
        {
            return UsersRepo.Query(u => u.Name.Contains(userNameToMatch));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userNameToMatch"></param>
        /// <returns></returns>
        public IQueryable<User> FindUsersInRole(string roleName, string userNameToMatch)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));
            Contract.Requires(!String.IsNullOrWhiteSpace(userNameToMatch));

            return UsersRepo.Query(u => u.Name.Contains(userNameToMatch) && u.Roles.Any(r => r.Name.ToLower() == roleName.ToLower()));
        }

        private string GeneratePassword()
        {
            // Variables
            StringBuilder stringBuilder = new StringBuilder();
            string Content = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!§$%&/()=?*#-";
            Random rnd = new Random();

            // Computations
            for (int i = 0; i < 8; i++)
            {
                stringBuilder.Append(Content[rnd.Next(Content.Length)]);
            }

            return stringBuilder.ToString();
        }

        private string GenerateSalt()
        {
            // Variables
            byte[] salt = new byte[32];
            new RNGCryptoServiceProvider().GetBytes(salt);

            // Computations
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IQueryable<Role> GetAllRoles()
        {
            return (RolesRepo.Query());
        }

        public IQueryable<Subject> GetAllSubjects()
        {
            return SubjectsRepo.Query();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IQueryable<User> GetAllUsers()
        {
            return UsersRepo.Query();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Role GetRoleById(long id)
        {
            ICollection<Role> roles = RolesRepo.Query(r => r.Id == id).ToArray();

            if (roles.Count() == 1)
            {
                return roles.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public Role GetRoleByName(string roleName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));

            ICollection<Role> roles = RolesRepo.Query(r => r.Name == roleName).ToArray();

            if (roles.Count() == 1)
            {
                return roles.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetRoleNameById(long id)
        {
            ICollection<Role> roles = RolesRepo.Query(r => r.Id == id).ToArray();

            if (roles.Count() == 1)
            {
                return roles.FirstOrDefault().Name;
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public IQueryable<Role> GetRolesFromUser(string userName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));

            return RolesRepo.Query(r => r.Users.Any(u => u.Name.ToLower() == userName.ToLower()));
        }

        private SecurityUser GetSecurityUserByName(string userName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));

            ICollection<SecurityUser> securityUsers = SecurityUsersRepo.Query(s => s.Name == userName).ToArray();

            if (securityUsers.Count() == 1)
            {
                return securityUsers.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public User GetUserByEmail(string email)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(email));

            ICollection<User> users = UsersRepo.Query(u => u.Email == email).ToArray();

            if (users.Count() == 1)
            {
                return users.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public User GetUserById(long id, bool isOnline = false)
        {
            ICollection<User> users = UsersRepo.Query(u => u.Id == id).ToArray();

            if (users.Count() == 1)
            {
                User user = users.FirstOrDefault();

                if (user != null && isOnline)
                {
                    user.LastActivityDate = DateTime.Now;

                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<User> usersRepo = uow.GetRepository<User>();
                        usersRepo.Put(user);
                        uow.Commit();
                    }
                }

                return user;
            }
            else
            {
                return null;
            }
        }

        public User GetUserByName(string userName, bool isOnline = false)
        {
            ICollection<User> users = UsersRepo.Query(u => u.Name == userName).ToArray();

            if (users.Count() == 1)
            {
                User user = users.FirstOrDefault();

                if (user != null && isOnline)
                {
                    user.LastActivityDate = DateTime.Now;

                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<User> usersRepo = uow.GetRepository<User>();
                        usersRepo.Put(user);
                        uow.Commit();
                    }
                }

                return user;
            }
            else
            {
                return null;
            }
        }

        public string GetUserNameByEmail(string email)
        {
            ICollection<User> users = UsersRepo.Query(u => u.Email == email).ToArray();

            if (users.Count() == 1)
            {
                return users.FirstOrDefault().Name;
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetUserNameById(long id)
        {
            Contract.Requires(id > 0);

            ICollection<User> users = UsersRepo.Query(u => u.Id == id).ToArray();

            if (users.Count() == 1)
            {
                return users.FirstOrDefault().Name;
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public IQueryable<User> GetUsersFromRole(string roleName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));

            return UsersRepo.Query(u => u.Roles.Any(r => r.Name.ToLower() == roleName.ToLower()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetUsersOnline()
        {
            TimeSpan timeFrame = new TimeSpan(0, OnlineWindow, 0);
            DateTime referenceTime = DateTime.Now.Subtract(timeFrame);

            return UsersRepo.Query(u => (DateTime.Compare(u.LastActivityDate, referenceTime) > 0)).Count();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public bool IsRoleInUse(string roleName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));

            if (GetUsersFromRole(roleName).Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public bool IsUserInRole(string userName, string roleName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));

            if (GetUsersFromRole(roleName).Where(u => u.Name.ToLower() == userName.ToLower()).Count() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public int RemoveUserFromRole(string userName, string roleName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(roleName));

            Role role = GetRoleByName(roleName);

            if (role != null)
            {
                User user = GetUserByName(userName);

                if (user != null)
                {
                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<Role> repo = uow.GetRepository<Role>();

                        repo.LoadIfNot(role.Users);
                        if (role.Users.Contains(user))
                        {
                            role.Users.Remove(user);
                            user.Roles.Remove(role);
                            uow.Commit();
                        }
                    }

                    return 0;
                }
                else
                {
                    return 12;
                }
            }
            else
            {
                return 11;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public int RemoveUserFromRole(long userId, long roleId)
        {
            Contract.Requires(userId > 0);
            Contract.Requires(roleId > 0);

            Role role = GetRoleById(roleId);

            if (role != null)
            {
                User user = GetUserById(userId);

                if (user != null)
                {
                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<Role> repo = uow.GetRepository<Role>();

                        repo.LoadIfNot(role.Users);
                        if (role.Users.Contains(user))
                        {
                            role.Users.Remove(user);
                            user.Roles.Remove(role);
                            uow.Commit();
                        }
                    }

                    return 0;
                }
                else
                {
                    return 12;
                }
            }
            else
            {
                return 11;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passwordAnswer"></param>
        /// <returns></returns>
        public string ResetPassword(string userName, string passwordAnswer)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(passwordAnswer));

            User user = GetUserByName(userName);
            SecurityUser securityUser = GetSecurityUserByName(userName);

            string password = String.Empty;

            if (user != null && securityUser != null)
            {
                if (ValidateSecurityProperty(passwordAnswer, securityUser.SecurityAnswer, DecodeSalt(securityUser.SecurityAnswerSalt)))
                {
                    password = GeneratePassword();

                    string passwordSalt = GenerateSalt();

                    securityUser.Password = EncodeSecurityProperty(password, passwordSalt);
                    securityUser.PasswordSalt = EncodeSalt(passwordSalt);
                    user.LastPasswordChangeDate = DateTime.Now;

                    using (IUnitOfWork uow = this.GetUnitOfWork())
                    {
                        IRepository<User> userRepo = uow.GetRepository<User>();
                        IRepository<SecurityUser> securityUserRepo = uow.GetRepository<SecurityUser>();

                        userRepo.Put(user);
                        securityUserRepo.Put(securityUser);

                        uow.Commit();
                    }
                }
                else
                {
                    UpdateFailureCount(user, FailureType.SecurityAnswer);
                }
            }

            return password;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool UnlockUser(string userName)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));

            User user = GetUserByName(userName);

            if (user != null)
            {
                user.IsLockedOut = false;

                using (IUnitOfWork uow = this.GetUnitOfWork())
                {
                    IRepository<User> repo = uow.GetRepository<User>();

                    repo.Put(user);
                    uow.Commit();
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void UpdateFailureCount(User user, FailureType failureType)
        {
            user = UsersRepo.Refresh(user.Id);
            SecurityUser securityUser = GetSecurityUserByName(user.Name);

            if (user != null && securityUser != null)
            {
                TimeSpan timeFrame;
                DateTime referenceTime;

                switch (failureType)
                {
                    case FailureType.Password:
                        timeFrame = new TimeSpan(0, PasswordFailureAttemptsWindow, 0);
                        referenceTime = DateTime.Now.Subtract(timeFrame);

                        if (DateTime.Compare(securityUser.LastPasswordFailureDate, referenceTime) > 0)
                        {
                            securityUser.LastPasswordFailureDate = DateTime.Now;
                            securityUser.PasswordFailureCount = securityUser.PasswordFailureCount + 1;

                            if (securityUser.PasswordFailureCount == MaxPasswordFailureAttempts)
                            {
                                user.LastLockOutDate = DateTime.Now;
                                user.IsLockedOut = true;
                            }

                            using (IUnitOfWork uow = this.GetUnitOfWork())
                            {
                                IRepository<User> usersRepo = uow.GetRepository<User>();
                                IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                                usersRepo.Put(user);
                                securityUsersRepo.Put(securityUser);

                                uow.Commit();
                            }
                        }
                        else
                        {
                            securityUser.LastPasswordFailureDate = DateTime.Now;
                            securityUser.PasswordFailureCount = 1;

                            using (IUnitOfWork uow = this.GetUnitOfWork())
                            {
                                IRepository<User> usersRepo = uow.GetRepository<User>();
                                IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                                usersRepo.Put(user);
                                securityUsersRepo.Put(securityUser);

                                uow.Commit();
                            }
                        }

                        break;
                    case FailureType.SecurityAnswer:
                        timeFrame = new TimeSpan(0, SecurityAnswerFailureAttemptsWindow, 0);
                        referenceTime = DateTime.Now.Subtract(timeFrame);

                        if (DateTime.Compare(securityUser.LastSecurityAnswerFailureDate, referenceTime) > 0)
                        {
                            user.LastActivityDate = DateTime.Now;
                            securityUser.LastSecurityAnswerFailureDate = DateTime.Now;
                            securityUser.SecurityAnswerFailureCount = securityUser.SecurityAnswerFailureCount + 1;

                            if (securityUser.SecurityAnswerFailureCount == MaxSecurityAnswerFailureAttempts)
                            {
                                user.LastLockOutDate = DateTime.Now;
                                user.IsLockedOut = true;
                            }

                            using (IUnitOfWork uow = this.GetUnitOfWork())
                            {
                                IRepository<User> usersRepo = uow.GetRepository<User>();
                                IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                                usersRepo.Put(user);
                                securityUsersRepo.Put(securityUser);

                                uow.Commit();
                            }
                        }
                        else
                        {
                            user.LastActivityDate = DateTime.Now;
                            securityUser.LastSecurityAnswerFailureDate = DateTime.Now;
                            securityUser.SecurityAnswerFailureCount = 1;

                            using (IUnitOfWork uow = this.GetUnitOfWork())
                            {
                                IRepository<User> usersRepo = uow.GetRepository<User>();
                                IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                                usersRepo.Put(user);
                                securityUsersRepo.Put(securityUser);

                                uow.Commit();
                            }
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public Role UpdateRole(Role role)
        {
            Contract.Requires(role != null);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Role> repo = uow.GetRepository<Role>();
                repo.Put(role);
                uow.Commit();
            }
            return (role);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public User UpdateUser(User user)
        {
            Contract.Requires(user != null);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<User> usersRepo = uow.GetRepository<User>();
                usersRepo.Put(user);
                uow.Commit();
            }

            return (user);
        }

        private bool ValidateSecurityProperty(string value, string referenceValue, string salt)
        {
            if (referenceValue == EncodeSecurityProperty(value, salt))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool ValidateUser(string userName, string password)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(userName));
            Contract.Requires(!String.IsNullOrWhiteSpace(password));

            bool validation = false;

            User user = GetUserByName(userName, true);

            if (user != null)
            {
                SecurityUser securityUser = GetSecurityUserByName(user.Name);

                if (securityUser != null)
                {
                    if (securityUser.PasswordFailureCount <= MaxPasswordFailureAttempts)
                    {
                        if (user.IsApproved && !user.IsLockedOut)
                        {
                            if (ValidateSecurityProperty(password, securityUser.Password, DecodeSalt(securityUser.PasswordSalt)))
                            {
                                validation = true;

                                user.LastLoginDate = DateTime.Now;

                                using (IUnitOfWork uow = this.GetUnitOfWork())
                                {
                                    IRepository<User> usersRepo = uow.GetRepository<User>();
                                    IRepository<SecurityUser> securityUsersRepo = uow.GetRepository<SecurityUser>();

                                    usersRepo.Put(user);
                                    securityUsersRepo.Put(securityUser);

                                    uow.Commit();
                                }
                            }
                            else
                            {
                                UpdateFailureCount(user, FailureType.Password);
                            }
                        }
                    }
                }
            }

            return validation;
        }

        #endregion
    }
}