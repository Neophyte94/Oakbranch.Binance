using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Validates the completeness of a parsed object based on a schema of tracked properties.
    /// </summary>
    internal class ParseSchemaValidator
    {
        private readonly int m_TotalCount;
        /// <summary>
        /// Gets the total number of properties tracked within this object schema.
        /// </summary>
        public int TotalCount => m_TotalCount;

        private int m_Mask;
        private int CompleteMask => (1 << m_TotalCount) - 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseSchemaValidator"/> class
        /// with the specified number of tracked properties.
        /// </summary>
        /// <param name="trackedPropCount">
        /// The total number of tracked properties.
        /// <para>The acceptable values range is from 0 to 30 inclusively.</para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public ParseSchemaValidator(int trackedPropCount)
        {
            if (trackedPropCount < 0 || trackedPropCount > 30)
                throw new ArgumentOutOfRangeException(nameof(trackedPropCount));
            m_TotalCount = trackedPropCount;
        }

        /// <summary>
        /// Registers a tracked property with the specified property number.
        /// </summary>
        /// <param name="propertyNumber">The property number to register (starting from 0).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the specified property number is less than 0 or exceeds the total number of tracked properties.
        /// </exception>
        public void RegisterProperty(int propertyNumber)
        {
            if (propertyNumber < 0 || propertyNumber >= m_TotalCount)
                throw new ArgumentOutOfRangeException(nameof(propertyNumber));
            m_Mask |= 1 << propertyNumber;
        }

        /// <summary>
        /// Checks whether all tracked properties have been registered.
        /// </summary>
        /// <returns><see langword="true"/> if all tracked properties have been registered, <see langword="false"/> otherwise.</returns>
        public bool IsComplete()
        {
            // Check whether the mask has all bits set.
            return m_Mask == CompleteMask;
        }

        /// <summary>
        /// Gets the number of the first missing property, or -1 if all properties have been registered.
        /// </summary>
        /// <returns>The number of the first missing property, or -1 if all properties have been registered.</returns>
        public int GetMissingPropertyNumber()
        {
            for (int i = 0; i != m_TotalCount; ++i)
            {
                if (((1 << i) & m_Mask) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Resets the validator by unregistering all tracked properties.
        /// </summary>
        public void Reset()
        {
            m_Mask = 0;
        }
    }
}
