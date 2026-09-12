import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "unity-g1.yml"


class UnityG1WorkflowTests(unittest.TestCase):
    def setUp(self):
        self.text = WORKFLOW.read_text(encoding="utf-8")

    def test_main_and_pull_request_changes_can_trigger_g1(self):
        self.assertIn("push:", self.text)
        self.assertIn("branches: [main]", self.text)
        self.assertIn("pull_request:", self.text)
        self.assertIn("Packages/**", self.text)
        self.assertIn("ProjectSettings/**", self.text)

    def test_self_hosted_job_is_disabled_until_editor_path_is_configured(self):
        self.assertIn("vars.UNITY_EDITOR_PATH != ''", self.text)
        self.assertIn("self-hosted", self.text)
        self.assertIn("unity-6000.3.24f1", self.text)

    def test_fork_pull_requests_cannot_enter_self_hosted_runner(self):
        self.assertIn("github.event.pull_request.head.repo.full_name == github.repository", self.text)

    def test_concurrency_prevents_duplicate_editor_runs_for_same_ref(self):
        self.assertIn("group: unity-g1-${{ github.ref }}", self.text)
        self.assertIn("cancel-in-progress: true", self.text)

    def test_logs_and_results_are_uploaded_even_when_g1_fails(self):
        self.assertIn("if: always()", self.text)
        self.assertIn("actions/upload-artifact@v7", self.text)
        self.assertIn("Artifacts/unity-g1/**", self.text)
        self.assertIn("retention-days: 14", self.text)


if __name__ == "__main__":
    unittest.main()
