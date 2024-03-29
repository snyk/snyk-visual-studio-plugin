﻿namespace Snyk.Code.Library.Api.Dto
{
    /// <summary>
    /// SnykCode login response result.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// Gets or sets a value indicating Session token.
        /// </summary>
        public string SessionToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating login URL.
        /// </summary>
        public string LoginURL { get; set; }

        /// <summary>
        /// Gets a value indicating whether is success login.
        /// Check is error null or empty and session token not null or empty.
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(this.Error) && !string.IsNullOrEmpty(this.SessionToken);

        private string Error { get; set; }
    }
}
