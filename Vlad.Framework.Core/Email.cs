﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Aspectacular
{
    public enum EmailAddressParts
    {
        /// <summary>
        /// In john.doe+junk@domain.com, it's "john.doe+junk"
        /// </summary>
        UserBeforeAt,

        /// <summary>
        /// In john.doe+spam@domain.com, it's "john.doe"
        /// </summary>
        UserBeforePlus,

        /// <summary>
        /// In john.doe+spam@domain.com, it's "spam"
        /// </summary>
        UserAfterPlusFilter,

        /// <summary>
        /// In john.doe+spam@first.domain.com, it's "first.domain.com"
        /// </summary>
        Domain,

        /// <summary>
        /// In john.doe+spam@first.domain.com, it's "first.domain"
        /// </summary>
        DomainMain,

        /// <summary>
        /// In john.doe+spam@first.domain.com, it's "com"
        /// </summary>
        DomainSuffix,
    }

    /// <summary>
    /// Smart class can be used a substitute for "string emailAddress;".
    /// Has implicit conversion operators from and to string and thus can be used in method parameters for email addresses.
    /// </summary>
    public class EmailAddress : IEquatable<EmailAddress>, IEquatable<string>, IComparable, IComparable<EmailAddress>, IComparable<string>
    {
        /// <summary>
        /// Global email address format check regular expression pattern.
        /// I suspect it will be continually improved and updated.
        /// </summary>
        public static readonly string emailCheckRegexPattern = @"(?<UserBeforeAt> (?<UserBeforePlus> [^@\+]{2,} )  (?: \+ (?<UserAfterPlusFilter> [^@]{1,} )){0,1}  ) @  (?<Domain> (?<DomainMain>.{2,})  \. (?<DomainSuffix> \w{2,6}) )".Replace(" ", string.Empty);

        /// <summary>
        /// Global email address format check regular expression.
        /// </summary>
        public static Regex emailFormatRegex = new Regex(emailCheckRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public readonly Match Match; 

        public EmailAddress(string emailAddress)
        {
            this.Match = ParseEmailAddress(emailAddress);
        }

        /// <summary>
        /// Returns true if parsed string was valid email address.
        /// </summary>
        public bool IsValid
        {
            get { return this.Match != null && this.Match.Success; }
        }

        public static implicit operator EmailAddress(string emailAddress)
        {
            return new EmailAddress(emailAddress);
        }

        public static implicit operator string(EmailAddress parsedEmail)
        {
            return parsedEmail == null ? null : parsedEmail.FullAddress;
        }

        /// <summary>
        /// Returns null if parsed string was not of the valid email format.
        /// Otherwise return a part of an email address.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public string this[EmailAddressParts part]
        {
            get
            {
                return this.Match.GetGroupValue(part.ToString());
            }
        }

        /// <summary>
        /// Returns full email address by rebuilding it from parsed parts.
        /// Returns null if parsed string was not in the valid email format.
        /// </summary>
        public string FullAddress
        {
            get { return this.ToString(); }
        }

        /// <summary>
        /// Returns email address without "+whatever" part.
        /// For example, if source email address string was "johndoe+spam@doamin.com",
        /// this property will return "johndoe@doamin.com".
        /// Returns null if parsed string was not in the valid email format.
        /// </summary>
        public string AddressWithoutFilter
        {
            get { return !this.IsValid ? null : "{0}@{1}".SmartFormat(this[EmailAddressParts.UserBeforePlus], this[EmailAddressParts.Domain]); }
        }

        #region Utility methods

        public static Match ParseEmailAddress(string emailAddress)
        {
            if (emailAddress == null)
                emailAddress = string.Empty;

            return emailFormatRegex.Match(emailAddress.Trim());
        }

        
        #endregion Utility methods

        #region Virtual overrides and interface implementations

        /// <summary>
        /// Returns null if parsed string was not in the valid email address format.
        /// Otherwise, return full email address.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return !this.IsValid ? null : "{0}@{1}".SmartFormat(this[EmailAddressParts.UserBeforeAt], this[EmailAddressParts.Domain]);
        }

        public override int GetHashCode()
        {
            string stringEmail = this;
            return stringEmail == null ? 0 : stringEmail.GetHashCode();
        }

        public bool Equals(EmailAddress other)
        {
            return this.Equals(other.ToStringEx());
        }

        public bool Equals(string other)
        {
            string address = this;
            return (address == null && other == null) || address.Equals(other);
        }

        public int CompareTo(object obj)
        {
            string address = this;
            string other = null;

            if (obj != null)
            {
                if (obj is string)
                    other = (string)obj;
                else if (obj is EmailAddress)
                    other = (EmailAddress)obj;
                else
                    throw new Exception("EmailAddress cannot be compared to \"{0}\".".SmartFormat(obj.GetType().FormatCSharp()));
            }

            if (address == null)
                return other == null ? 0 : int.MinValue;

            return address.CompareTo(other);
        }

        public int CompareTo(EmailAddress other)
        {
            return this.CompareTo((object)other);
        }

        public int CompareTo(string other)
        {
            return this.CompareTo((object)other);
        }

        #endregion Virtual overrides and interface implementations
    }

    public static class EmailHelper
    {
        /// <summary>
        /// Sends SMTP email using .config file settings.
        /// </summary>
        /// <param name="isBodyHtml"></param>
        /// <param name="optioanlFromAddress">If null, .config from address value is used.</param>
        /// <param name="optionalReplyToAddress">If null, reply-to address is the same as from address.</param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="toAddresses"></param>
        public static void SendSmtpEmail(bool isBodyHtml, NonEmptyString optioanlFromAddress, NonEmptyString optionalReplyToAddress, string subject, string body, params string[] toAddresses)
        {
            if (toAddresses != null)
                toAddresses = toAddresses.Where(addr => !addr.IsBlank()).ToArray();

            if (toAddresses.IsNullOrEmpty())
                throw new Exception("\"To\" address must be specified");

            if (subject.IsBlank() && body.IsBlank())
                throw new Exception("Both subject and message body cannot be blank.");

            MailMessage message = new MailMessage();

            if (optioanlFromAddress != null)
                message.From = new MailAddress(optioanlFromAddress);

            if (optionalReplyToAddress != null)
                message.ReplyToList.Add(new MailAddress(optionalReplyToAddress));

            toAddresses.ForEach(toAddr => message.To.Add(new MailAddress(toAddr)));

            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isBodyHtml;

            SmtpClient smtpClient = new SmtpClient(); // { Timeout = 10 * 1000 };
            smtpClient.Send(message);
        }

        /// <summary>
        /// Returns true if text matches email address regular expression pattern.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static bool IsValidEmailFormat(this string emailAddress)
        {
            if (emailAddress == null)
                return false;

            return EmailAddress.emailFormatRegex.IsMatch(emailAddress);
        }

        /// <summary>
        /// Same as EmailAddress, but handles null gracefully. Returns false if email is null.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValid(this EmailAddress email)
        {
            return email != null && email.IsValid;
        }
    }
}