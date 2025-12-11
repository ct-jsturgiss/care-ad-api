using CareAdApi.Models;
using System.DirectoryServices.Protocols;
using System.Runtime.ExceptionServices;
using ILogger = Serilog.ILogger;

namespace CareAdApi.Services
{
    public class ActiveDirectoryService
    {
        private ILogger m_logger = null!;

        public ActiveDirectoryService(ILogger logger)
        {
            m_logger = logger;
        }

        public async Task<AttributeUpdateResponse> UpdateAttributesAsync(IEnumerable<AttributesUpdate> updates)
        {
            try
            {
                AttributeUpdateResponse resp = new AttributeUpdateResponse();
                AttributesUpdate[] ups = updates.ToArray();

                using (LdapConnection lc = new LdapConnection(Constants.DomainTarget))
                {
                    lc.Bind();

                    foreach(AttributesUpdate update in ups)
                    {
                        try
                        {
                            m_logger.Information("Processing update for principal '{p}'", update.PrincipalName ?? string.Empty);
                            SearchResultEntry? targetUser = null;
                            SearchResultEntry? managerUser = null;
                            if (string.IsNullOrEmpty(update.PrincipalName))
                            {
                                resp.Errors.Add(new ProcessError()
                                {
                                    ErrorType = ErrorType.User,
                                    UserPrincipalName = update.PrincipalName ?? string.Empty,
                                    Messages = [$"No user principal was provided."]
                                });

                                continue;
                            }
                            else
                            {
                                targetUser = GetUserPrincipal(lc, update.PrincipalName!);
                                if(targetUser == null)
                                {
                                    resp.Errors.Add(new ProcessError()
                                    {
                                        ErrorType = ErrorType.User,
                                        UserPrincipalName = update.PrincipalName ?? string.Empty,
                                        Messages = [$"Could not locate user principal."]
                                    });

                                    continue;
                                }
                            }
                            if (!string.IsNullOrEmpty(update.ManagerPrincipalName))
                            {
                                managerUser = GetUserPrincipal(lc, update.ManagerPrincipalName);
                                if (managerUser == null)
                                {
                                    resp.Errors.Add(new ProcessError()
                                    {
                                        ErrorType = ErrorType.User,
                                        UserPrincipalName = update.PrincipalName ?? string.Empty,
                                        Messages = [$"Could not locate manager principal '{update.ManagerPrincipalName}'."]
                                    });

                                    continue;
                                }
                            }

                            if(string.IsNullOrEmpty(update.EmployeeId) && string.IsNullOrEmpty(update.ManagerPrincipalName))
                            {
                                resp.Errors.Add(new ProcessError()
                                {
                                    ErrorType = ErrorType.User,
                                    UserPrincipalName = update.PrincipalName ?? string.Empty,
                                    Messages = [$"No updates to '{Constants.AttrConstants.EmployeeId}' or '{Constants.AttrConstants.ManagerPrincipal}' have been provided."]
                                });

                                continue;
                            }

                            DirectoryAttribute[] userAttributes = targetUser.Attributes.Values.Cast<DirectoryAttribute>().ToArray();

                            DirectoryAttribute? attrEmployeeId = userAttributes.FirstOrDefault(x => x.Name.Equals(Constants.AttrConstants.EmployeeId, StringComparison.OrdinalIgnoreCase));
                            DirectoryAttribute? attrManager = userAttributes.FirstOrDefault(x => x.Name.Equals(Constants.AttrConstants.ManagerPrincipal, StringComparison.OrdinalIgnoreCase));
                            if (attrEmployeeId != null)
                            {
                                string id = attrEmployeeId[0] as string ?? string.Empty;
                                m_logger.Information("User principal: '{p}'; Existing Employee Id: '{id}'", update.PrincipalName, id);
                            }
                            if (!string.IsNullOrEmpty(update.EmployeeId))
                            {
                                DirectoryAttributeModification attrEmpId = new DirectoryAttributeModification();
                                attrEmpId.Name = Constants.AttrConstants.EmployeeId;
                                attrEmpId.Add(update.EmployeeId);
                                ModifyRequest mr = new ModifyRequest(targetUser.DistinguishedName, attrEmpId);
                                m_logger.Information("Updating attribute '{a}' for user principal '{p}'", Constants.AttrConstants.EmployeeId, update.PrincipalName);
                                m_logger.Debug("## SEND REQUEST DISABLED ##");
                                //lc.SendRequest(mr);
                                m_logger.Information("User principal: '{p}'; New Employee Id: '{id}'", update.PrincipalName, update.EmployeeId);
                            }
                            if (!string.IsNullOrEmpty(update.ManagerPrincipalName))
                            {
                                if (attrManager != null)
                                {
                                    string dn = attrManager[0] as string ?? string.Empty;
                                    string? firstCn = GetCommonName(dn);

                                    m_logger.Information("User principal: '{p}'; Manager: '{id}'", update.PrincipalName, firstCn ?? dn);
                                }
                                if (managerUser != null)
                                {
                                    DirectoryAttributeModification attrManagerName = new DirectoryAttributeModification();
                                    attrManagerName.Name = Constants.AttrConstants.ManagerPrincipal;
                                    attrManagerName.Add(managerUser.DistinguishedName);
                                    ModifyRequest mr = new ModifyRequest(targetUser.DistinguishedName, attrManagerName);
                                    m_logger.Information("Updating attribute '{a}' for user principal '{p}'", Constants.AttrConstants.ManagerPrincipal, update.PrincipalName);
                                    m_logger.Debug("## SEND REQUEST DISABLED ##");
                                    //lc.SendRequest(mr);
                                    string? newManagerCn = GetCommonName(managerUser.DistinguishedName);
                                    m_logger.Information("User principal: '{p}'; New Manager: '{id}'", update.PrincipalName, newManagerCn ?? managerUser.DistinguishedName);
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            resp.Errors.Add(new ProcessError()
                            {
                                ErrorType = ErrorType.Unknown,
                                UserPrincipalName = update.PrincipalName ?? string.Empty,
                                Messages = [$"Unexpected error: {ex.Message}"]
                            });
                        }
                    }
                }

                return resp;
            }
            catch(Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return null;
            }
        }

        private string? GetCommonName(string distinguishedName)
        {
            List<string[]> parts = distinguishedName.Split(",").Select(x => x.Split("=")).ToList();
            string? firstCn = parts.FirstOrDefault(x => x[0] == "CN")?[1];
            
            return firstCn;
        }

        private SearchResultEntry? GetUserPrincipal(LdapConnection connection, string principalName)
        {
            SearchRequest sr = new SearchRequest("OU=Users,OU=MyBusiness,DC=caretaker,DC=local",
                $"(&(objectCategory=person)(userPrincipalName={principalName}))",
            System.DirectoryServices.Protocols.SearchScope.Subtree, ["userPrincipalName", "employeeId", "mail", "manager"]);
            SearchResponse response = (SearchResponse)connection.SendRequest(sr);

            return response.Entries.Cast<SearchResultEntry>().FirstOrDefault();
        }
    }
}
