using System;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.DynamoDBv2;
using UnityEngine;

public static class ErrorHandler
{
    // HANDLE AUTH EXCEPTIONS
    // Converts raw AWS Cognito exceptions into clean AuthResult failures
    public static AuthResult HandleAuthException(Exception e)
    {
        Debug.LogError($"[ErrorHandler] Auth exception: {e.GetType().Name}, {e.Message}");

        // Cognito specific exceptions
        if (e is UsernameExistsException)
            return AuthResult.Failure(AuthError.EmailAlreadyExists);

        if (e is InvalidPasswordException)
            return AuthResult.Failure(AuthError.WeakPassword);

        if (e is InvalidParameterException)
            return AuthResult.Failure(AuthError.InvalidInput, e.Message);

        if (e is UserNotFoundException)
            return AuthResult.Failure(AuthError.UserNotFound);

        if (e is UserNotConfirmedException)
            return AuthResult.Failure(AuthError.EmailNotConfirmed);

        if (e is NotAuthorizedException)
            return AuthResult.Failure(AuthError.WrongEmailOrPassword);

        if (e is CodeMismatchException)
            return AuthResult.Failure(AuthError.InvalidConfirmationCode);

        if (e is ExpiredCodeException)
            return AuthResult.Failure(AuthError.ExpiredConfirmationCode);

        if (e is TooManyRequestsException)
            return AuthResult.Failure(AuthError.TooManyRequests);

        if (e is TooManyFailedAttemptsException)
            return AuthResult.Failure(AuthError.TooManyRequests);

        if (e is LimitExceededException)
            return AuthResult.Failure(AuthError.TooManyRequests);

        // Network and service exceptions
        if (e is AmazonServiceException amazonEx)
        {
            if (amazonEx.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                return AuthResult.Failure(AuthError.ServiceUnavailable);

            if (amazonEx.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                return AuthResult.Failure(AuthError.NetworkError);
        }

        if (e is AmazonClientException)
            return AuthResult.Failure(AuthError.NetworkError);

        // Fallback for anything not caught above
        return AuthResult.Failure(AuthError.UnknownError, e.Message);
    }

    // HANDLE S3 EXCEPTIONS
    // Converts raw S3 exceptions into clean readable messages
    public static string HandleS3Exception(Exception e)
    {
        Debug.LogError($"[ErrorHandler] S3 exception: {e.GetType().Name}, {e.Message}");

        if (e is AmazonS3Exception s3Ex)
        {
            switch (s3Ex.ErrorCode)
            {
                case "NoSuchBucket":
                    return "Storage bucket not found. Please contact support.";
                case "NoSuchKey":
                    return "The requested file was not found.";
                case "AccessDenied":
                    return "You do not have permission to access this file.";
                case "EntityTooLarge":
                    return "File is too large to upload.";
                case "InvalidBucketName":
                    return "Invalid storage configuration. Please contact support.";
                default:
                    return "A storage error occurred. Please try again.";
            }
        }

        if (e is AmazonClientException)
            return "Network error. Please check your internet connection.";

        return "An unexpected storage error occurred. Please try again.";
    }

    // HANDLE DYNAMODB EXCEPTIONS
    // Converts raw DynamoDB exceptions into clean readable messages
    public static string HandleDynamoDBException(Exception e)
    {
        Debug.LogError($"[ErrorHandler] DynamoDB exception: {e.GetType().Name}, {e.Message}");

        if (e is AmazonDynamoDBException dynamoEx)
        {
            switch (dynamoEx.ErrorCode)
            {
                case "ResourceNotFoundException":
                    return "Data table not found. Please contact support.";
                case "ProvisionedThroughputExceededException":
                    return "Service is busy. Please try again in a moment.";
                case "ConditionalCheckFailedException":
                    return "This record was updated by another session. Please refresh and try again.";
                case "AccessDeniedException":
                    return "You do not have permission to access this data.";
                default:
                    return "A database error occurred. Please try again.";
            }
        }

        if (e is AmazonClientException)
            return "Network error. Please check your internet connection.";

        return "An unexpected database error occurred. Please try again.";
    }

    // HANDLE GENERAL EXCEPTIONS
    // Catches anything not covered by the specific handlers above
    public static string HandleGeneralException(Exception e)
    {
        Debug.LogError($"[ErrorHandler] General exception: {e.GetType().Name}, {e.Message}");

        if (e is AmazonClientException)
            return "Network error. Please check your internet connection and try again.";

        if (e is AmazonServiceException)
            return "Service is temporarily unavailable. Please try again shortly.";

        if (e is OperationCanceledException)
            return "The operation was cancelled. Please try again.";

        if (e is TimeoutException)
            return "The request timed out. Please check your connection and try again.";

        return "Something went wrong. Please try again.";
    }

    // LOG WARNING
    // Logs non critical issues that do not break the app
    public static void LogWarning(string context, string message)
    {
        Debug.LogWarning($"[{context}] {message}");
    }

    // LOG ERROR
    // Logs critical issues that break a feature
    public static void LogError(string context, Exception e)
    {
        Debug.LogError($"[{context}] {e.GetType().Name}: {e.Message}");
    }
}