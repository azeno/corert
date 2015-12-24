// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using global::System;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;
using global::System.Reflection.Runtime.General;
using global::System.Reflection.Runtime.CustomAttributes;

using global::Internal.Reflection.Core;
using global::Internal.Reflection.Core.Execution;
using global::Internal.Reflection.Core.NonPortable;

using global::Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.ParameterInfos
{
    //
    // This implements ParameterInfo objects owned by MethodBase objects that have an associated Parameter metadata entity.
    //
    internal sealed partial class RuntimeFatMethodParameterInfo : RuntimeMethodParameterInfo
    {
        private RuntimeFatMethodParameterInfo(MethodBase member, MethodHandle methodHandle, int position, ParameterHandle parameterHandle, ReflectionDomain reflectionDomain, MetadataReader reader, Handle typeHandle, TypeContext typeContext)
            : base(member, position, reflectionDomain, reader, typeHandle, typeContext)
        {
            _methodHandle = methodHandle;
            _parameterHandle = parameterHandle;
            _parameter = parameterHandle.GetParameter(reader);
        }

        public sealed override ParameterAttributes Attributes
        {
            get
            {
                return _parameter.Flags;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                ReflectionDomain reflectionDomain = this.ReflectionDomain;
                IEnumerable<CustomAttributeData> customAttributes = RuntimeCustomAttributeData.GetCustomAttributes(reflectionDomain, this.Reader, _parameter.CustomAttributes);
                foreach (CustomAttributeData cad in customAttributes)
                    yield return cad;
                ExecutionDomain executionDomain = reflectionDomain as ExecutionDomain;
                if (executionDomain != null)
                {
                    MethodHandle declaringMethodHandle = _methodHandle;
                    foreach (CustomAttributeData cad in executionDomain.ExecutionEnvironment.GetPsuedoCustomAttributes(this.Reader, _parameterHandle, declaringMethodHandle))
                        yield return cad;
                }
            }
        }

        public sealed override Object DefaultValue
        {
            get
            {
                return DefaultValueInfo.Item2;
            }
        }

        public sealed override bool HasDefaultValue
        {
            get
            {
                return DefaultValueInfo.Item1;
            }
        }

        public sealed override String Name
        {
            get
            {
                return _parameter.Name.GetStringOrNull(this.Reader);
            }
        }

        private Tuple<bool, Object> DefaultValueInfo
        {
            get
            {
                Tuple<bool, Object> defaultValueInfo = _lazyDefaultValueInfo;
                if (defaultValueInfo == null)
                {
                    if (!(this.ReflectionDomain is ExecutionDomain))
                        throw new NotSupportedException();

                    Object defaultValue;
                    bool hasDefaultValue = ReflectionCoreExecution.ExecutionEnvironment.GetDefaultValueIfAny(
                        this.Reader,
                        _parameterHandle,
                        this.ParameterType,
                        this.CustomAttributes,
                        out defaultValue);
                    if (!hasDefaultValue)
                    {
                        defaultValue = IsOptional ? Missing.Value : DBNull.Value;
                    }
                    defaultValueInfo = _lazyDefaultValueInfo = Tuple.Create(hasDefaultValue, defaultValue);
                }
                return defaultValueInfo;
            }
        }

        private MethodHandle _methodHandle;
        private ParameterHandle _parameterHandle;
        private Parameter _parameter;
        private volatile Tuple<bool, Object> _lazyDefaultValueInfo;
    }
}
