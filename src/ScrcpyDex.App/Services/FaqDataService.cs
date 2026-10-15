// Copyright 2026 Aureliano Peixoto and ScrcpyDeX Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using ScrcpyDex.App.Models;

namespace ScrcpyDex.App.Services
{
    /// <summary>
    /// Provides repository catalog for troubleshooting and FAQ entries.
    /// Maps 1:1 against the ScrcpyDeX troubleshooting specification.
    /// </summary>
    public static class FaqDataService
    {
        public static IReadOnlyList<FaqItem> CreateFaqItems()
        {
            return new List<FaqItem>
            {
                // Category A: Connection & ADB
                new FaqItem
                {
                    Id = 1,
                    CategoryId = "Connection",
                    TitleKey = "Faq_Q1_Title",
                    SymptomKey = "Faq_Q1_Symptom",
                    CauseKey = "Faq_Q1_Cause",
                    SolutionKey = "Faq_Q1_Solution",
                    CommandCode = null
                },
                new FaqItem
                {
                    Id = 2,
                    CategoryId = "Connection",
                    TitleKey = "Faq_Q2_Title",
                    SymptomKey = "Faq_Q2_Symptom",
                    CauseKey = "Faq_Q2_Cause",
                    SolutionKey = "Faq_Q2_Solution",
                    CommandCode = null
                },
                new FaqItem
                {
                    Id = 3,
                    CategoryId = "Connection",
                    TitleKey = "Faq_Q3_Title",
                    SymptomKey = "Faq_Q3_Symptom",
                    CauseKey = "Faq_Q3_Cause",
                    SolutionKey = "Faq_Q3_Solution",
                    CommandCode = null
                },

                // Category B: Samsung DeX Mode
                new FaqItem
                {
                    Id = 4,
                    CategoryId = "DeX",
                    TitleKey = "Faq_Q4_Title",
                    SymptomKey = "Faq_Q4_Symptom",
                    CauseKey = "Faq_Q4_Cause",
                    SolutionKey = "Faq_Q4_Solution",
                    CommandCode = null
                },
                new FaqItem
                {
                    Id = 5,
                    CategoryId = "DeX",
                    TitleKey = "Faq_Q5_Title",
                    SymptomKey = "Faq_Q5_Symptom",
                    CauseKey = "Faq_Q5_Cause",
                    SolutionKey = "Faq_Q5_Solution",
                    CommandCode = "adb shell rm -f /data/local/tmp/scrcpydex-server.jar"
                },

                // Category C: Display & Render Drivers
                new FaqItem
                {
                    Id = 6,
                    CategoryId = "Display",
                    TitleKey = "Faq_Q6_Title",
                    SymptomKey = "Faq_Q6_Symptom",
                    CauseKey = "Faq_Q6_Cause",
                    SolutionKey = "Faq_Q6_Solution",
                    CommandCode = null
                },
                new FaqItem
                {
                    Id = 7,
                    CategoryId = "Display",
                    TitleKey = "Faq_Q7_Title",
                    SymptomKey = "Faq_Q7_Symptom",
                    CauseKey = "Faq_Q7_Cause",
                    SolutionKey = "Faq_Q7_Solution",
                    CommandCode = "sudo apt install va-driver-all i965-va-driver intel-media-va-driver vainfo mesa-va-drivers"
                },
                new FaqItem
                {
                    Id = 8,
                    CategoryId = "Display",
                    TitleKey = "Faq_Q8_Title",
                    SymptomKey = "Faq_Q8_Symptom",
                    CauseKey = "Faq_Q8_Cause",
                    SolutionKey = "Faq_Q8_Solution",
                    CommandCode = null
                },

                // Category D: Peripherals & Audio
                new FaqItem
                {
                    Id = 9,
                    CategoryId = "Input",
                    TitleKey = "Faq_Q9_Title",
                    SymptomKey = "Faq_Q9_Symptom",
                    CauseKey = "Faq_Q9_Cause",
                    SolutionKey = "Faq_Q9_Solution",
                    CommandCode = null
                },
                new FaqItem
                {
                    Id = 10,
                    CategoryId = "Input",
                    TitleKey = "Faq_Q10_Title",
                    SymptomKey = "Faq_Q10_Symptom",
                    CauseKey = "Faq_Q10_Cause",
                    SolutionKey = "Faq_Q10_Solution",
                    CommandCode = null
                },
                new FaqItem
                {
                    Id = 11,
                    CategoryId = "Input",
                    TitleKey = "Faq_Q11_Title",
                    SymptomKey = "Faq_Q11_Symptom",
                    CauseKey = "Faq_Q11_Cause",
                    SolutionKey = "Faq_Q11_Solution",
                    CommandCode = null
                },

                // Category E: Battery & Thermal
                new FaqItem
                {
                    Id = 12,
                    CategoryId = "Battery",
                    TitleKey = "Faq_Q12_Title",
                    SymptomKey = "Faq_Q12_Symptom",
                    CauseKey = "Faq_Q12_Cause",
                    SolutionKey = "Faq_Q12_Solution",
                    CommandCode = null
                }
            };
        }

        public static IReadOnlyList<FaqCategoryItem> CreateCategories()
        {
            return new List<FaqCategoryItem>
            {
                new FaqCategoryItem { Id = "All", NameKey = "Faq_Category_All", Count = 12, IsSelected = true },
                new FaqCategoryItem { Id = "Connection", NameKey = "Faq_Category_Connection", Count = 3, IsSelected = false },
                new FaqCategoryItem { Id = "DeX", NameKey = "Faq_Category_DeX", Count = 2, IsSelected = false },
                new FaqCategoryItem { Id = "Display", NameKey = "Faq_Category_Display", Count = 3, IsSelected = false },
                new FaqCategoryItem { Id = "Input", NameKey = "Faq_Category_Input", Count = 3, IsSelected = false },
                new FaqCategoryItem { Id = "Battery", NameKey = "Faq_Category_Battery", Count = 1, IsSelected = false }
            };
        }
    }
}
