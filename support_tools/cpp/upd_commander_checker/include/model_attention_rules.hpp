#pragma once

#include <filesystem>
#include <string>
#include <vector>

#include "ignore_rules.hpp"
#include "models.hpp"

namespace upd_checker {

struct ModelGroupOccurrence {
    std::string path;
    int line = 1;
    std::string application_id;
    std::string layer;
    std::string kind;
    std::vector<std::string> items;

    std::string signature() const;
};

std::vector<ModelGroupOccurrence> collect_cpp_model_group_occurrences(
    const std::filesystem::path& path,
    const std::filesystem::path& root,
    const std::string& relative,
    const std::vector<IgnoreRule>& rules,
    int min_items);

std::vector<Finding> model_attention_findings(
    const std::vector<ModelGroupOccurrence>& occurrences,
    int min_occurrences);

}  // namespace upd_checker
